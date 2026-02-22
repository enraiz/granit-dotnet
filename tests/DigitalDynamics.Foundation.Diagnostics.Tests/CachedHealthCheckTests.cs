using DigitalDynamics.Foundation.Diagnostics.Caching;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Xunit;

namespace DigitalDynamics.Foundation.Diagnostics.Tests;

public sealed class CachedHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsInnerResult_WhenCacheIsEmpty()
    {
        IHealthCheck inner = Substitute.For<IHealthCheck>();
        inner.CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(HealthCheckResult.Healthy());

        CachedHealthCheck sut = new(inner, TimeSpan.FromSeconds(10));
        HealthCheckContext context = BuildContext();

        HealthCheckResult result = await sut.CheckHealthAsync(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Healthy);
        await inner.Received(1).CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsCachedResult_WithoutCallingInner()
    {
        IHealthCheck inner = Substitute.For<IHealthCheck>();
        inner.CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(HealthCheckResult.Healthy());

        CachedHealthCheck sut = new(inner, TimeSpan.FromSeconds(30));
        HealthCheckContext context = BuildContext();

        await sut.CheckHealthAsync(context, TestContext.Current.CancellationToken);
        HealthCheckResult cached = await sut.CheckHealthAsync(context, TestContext.Current.CancellationToken);

        cached.Status.Should().Be(HealthStatus.Healthy);
        await inner.Received(1).CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_ExecutesInnerOnce_WhenCalledConcurrently()
    {
        int callCount = 0;
        IHealthCheck inner = Substitute.For<IHealthCheck>();
        inner.CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                Interlocked.Increment(ref callCount);
                await Task.Delay(50); // simulate slow dependency
                return HealthCheckResult.Healthy();
            });

        CachedHealthCheck sut = new(inner, TimeSpan.FromSeconds(30));
        HealthCheckContext context = BuildContext();

        // 10 concurrent callers — only 1 should hit the inner check
        IEnumerable<Task<HealthCheckResult>> tasks = Enumerable.Range(0, 10)
            .Select(_ => sut.CheckHealthAsync(context, TestContext.Current.CancellationToken));

        HealthCheckResult[] results = await Task.WhenAll(tasks);

        callCount.Should().Be(1);
        results.Should().AllSatisfy(r => r.Status.Should().Be(HealthStatus.Healthy));
    }

    [Fact]
    public async Task CheckHealthAsync_ReExecutesInner_AfterCacheExpires()
    {
        IHealthCheck inner = Substitute.For<IHealthCheck>();
        inner.CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(HealthCheckResult.Healthy());

        // Very short TTL so it expires immediately
        CachedHealthCheck sut = new(inner, TimeSpan.FromMilliseconds(1));
        HealthCheckContext context = BuildContext();

        await sut.CheckHealthAsync(context, TestContext.Current.CancellationToken);
        await Task.Delay(10, TestContext.Current.CancellationToken); // wait for TTL to expire
        await sut.CheckHealthAsync(context, TestContext.Current.CancellationToken);

        await inner.Received(2).CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_CachesDegradedResult()
    {
        IHealthCheck inner = Substitute.For<IHealthCheck>();
        inner.CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(HealthCheckResult.Degraded("slow dependency"));

        CachedHealthCheck sut = new(inner, TimeSpan.FromSeconds(30));
        HealthCheckContext context = BuildContext();

        await sut.CheckHealthAsync(context, TestContext.Current.CancellationToken);
        HealthCheckResult result = await sut.CheckHealthAsync(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Degraded);
        await inner.Received(1).CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>());
    }

    private static HealthCheckContext BuildContext() =>
        new() { Registration = new HealthCheckRegistration("test", _ => Substitute.For<IHealthCheck>(), null, []) };
}
