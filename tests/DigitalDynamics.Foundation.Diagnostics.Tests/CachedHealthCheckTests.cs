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

    [Fact]
    public void Dispose_ReleasesLock_WithoutThrowing()
    {
        // Arrange
        IHealthCheck inner = Substitute.For<IHealthCheck>();
        CachedHealthCheck sut = new(inner, TimeSpan.FromSeconds(10));

        // Act & Assert — Dispose must not throw; SemaphoreSlim is released
        Action act = sut.Dispose;
        act.Should().NotThrow();
    }

    [Fact]
    public async Task CheckHealthAsync_SecondConcurrentCaller_UsesDoubleCheckLockAndReturnsCachedResult()
    {
        // This test explicitly covers the double-check path (line 50):
        // Two callers enter before the cache is warm. The first acquires the lock,
        // populates the cache, then releases it. The second acquires the lock, hits
        // the double-check (cache is now warm), and returns the cached result without
        // calling inner again.
        SemaphoreSlim firstCallerStarted = new(0, 1);
        SemaphoreSlim firstCallerCanContinue = new(0, 1);
        int callCount = 0;

        IHealthCheck inner = Substitute.For<IHealthCheck>();
        inner.CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                Interlocked.Increment(ref callCount);
                firstCallerStarted.Release();
                await firstCallerCanContinue.WaitAsync();
                return HealthCheckResult.Healthy("populated");
            });

        CachedHealthCheck sut = new(inner, TimeSpan.FromSeconds(30));
        HealthCheckContext context = BuildContext();

        // First caller takes the lock and waits
        Task<HealthCheckResult> firstCall = sut.CheckHealthAsync(context, TestContext.Current.CancellationToken);

        // Wait until inner is executing (lock is held by first caller)
        await firstCallerStarted.WaitAsync(TestContext.Current.CancellationToken);

        // Second caller tries to enter — it will block on WaitAsync
        Task<HealthCheckResult> secondCall = sut.CheckHealthAsync(context, TestContext.Current.CancellationToken);

        // Let first caller finish, which populates the cache and releases the lock
        firstCallerCanContinue.Release();
        HealthCheckResult firstResult = await firstCall;

        // Second caller gets the lock, hits the double-check, and returns cached result
        HealthCheckResult secondResult = await secondCall;

        // Only one call to inner
        callCount.Should().Be(1);
        firstResult.Status.Should().Be(HealthStatus.Healthy);
        secondResult.Status.Should().Be(HealthStatus.Healthy);
    }

    private static HealthCheckContext BuildContext() =>
        new() { Registration = new HealthCheckRegistration("test", _ => Substitute.For<IHealthCheck>(), null, []) };
}
