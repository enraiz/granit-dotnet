using Granit.Identity.Endpoints.Internal;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Granit.Identity.Endpoints.Tests;

/// <summary>
/// Unit tests for the <see cref="UserCacheHealthCheck"/>.
/// </summary>
public sealed class UserCacheHealthCheckTests
{
    private readonly IUserCacheStats _cacheStats = Substitute.For<IUserCacheStats>();

    private UserCacheHealthCheck CreateHealthCheck() => new(_cacheStats);

    [Fact]
    public async Task Healthy_when_cache_empty()
    {
        _cacheStats.GetCountAsync(Arg.Any<CancellationToken>()).Returns(0);

        HealthCheckResult result = await CreateHealthCheck()
            .CheckHealthAsync(null!, TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Healthy_when_low_stale_ratio()
    {
        _cacheStats.GetCountAsync(Arg.Any<CancellationToken>()).Returns(100);
        _cacheStats.GetStaleCountAsync(Arg.Any<CancellationToken>()).Returns(5);

        HealthCheckResult result = await CreateHealthCheck()
            .CheckHealthAsync(null!, TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Degraded_when_stale_ratio_above_10_percent()
    {
        _cacheStats.GetCountAsync(Arg.Any<CancellationToken>()).Returns(100);
        _cacheStats.GetStaleCountAsync(Arg.Any<CancellationToken>()).Returns(20);

        HealthCheckResult result = await CreateHealthCheck()
            .CheckHealthAsync(null!, TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task Unhealthy_when_stale_ratio_above_50_percent()
    {
        _cacheStats.GetCountAsync(Arg.Any<CancellationToken>()).Returns(100);
        _cacheStats.GetStaleCountAsync(Arg.Any<CancellationToken>()).Returns(60);

        HealthCheckResult result = await CreateHealthCheck()
            .CheckHealthAsync(null!, TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Unhealthy_when_store_throws()
    {
        _cacheStats.GetCountAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB down"));

        HealthCheckResult result = await CreateHealthCheck()
            .CheckHealthAsync(null!, TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }
}
