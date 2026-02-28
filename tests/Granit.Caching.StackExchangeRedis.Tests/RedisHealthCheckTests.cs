// =============================================================================
// Tests - RedisHealthCheck
// =============================================================================
// Vérifie les 3 branches de CheckHealthAsync :
//   - Latence < threshold → Healthy
//   - Latence >= threshold → Degraded avec message de latence
//   - Exception → Unhealthy avec message sanitisé (type uniquement)
// =============================================================================

using Granit.Caching.StackExchangeRedis.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using StackExchange.Redis;
using Xunit;

namespace Granit.Caching.StackExchangeRedis.Tests;

public sealed class RedisHealthCheckTests
{
    private static HealthCheckContext BuildContext() =>
        new() { Registration = new HealthCheckRegistration("redis", _ => Substitute.For<IHealthCheck>(), null, []) };

    [Fact]
    public async Task CheckHealthAsync_LatencyBelowThreshold_ReturnsHealthy()
    {
        // Arrange
        IConnectionMultiplexer connection = Substitute.For<IConnectionMultiplexer>();
        IDatabase database = Substitute.For<IDatabase>();
        connection.GetDatabase().Returns(database);
        database.PingAsync(Arg.Any<CommandFlags>()).Returns(TimeSpan.FromMilliseconds(10));

        RedisHealthCheck sut = new(connection, TimeSpan.FromMilliseconds(100));

        // Act
        HealthCheckResult result = await sut.CheckHealthAsync(BuildContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("10");
    }

    [Fact]
    public async Task CheckHealthAsync_LatencyAtThreshold_ReturnsDegraded()
    {
        // Arrange
        IConnectionMultiplexer connection = Substitute.For<IConnectionMultiplexer>();
        IDatabase database = Substitute.For<IDatabase>();
        connection.GetDatabase().Returns(database);
        database.PingAsync(Arg.Any<CommandFlags>()).Returns(TimeSpan.FromMilliseconds(100));

        RedisHealthCheck sut = new(connection, TimeSpan.FromMilliseconds(100));

        // Act
        HealthCheckResult result = await sut.CheckHealthAsync(BuildContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("100");
        result.Description!.ShouldContain("threshold");
    }

    [Fact]
    public async Task CheckHealthAsync_LatencyAboveThreshold_ReturnsDegraded()
    {
        // Arrange
        IConnectionMultiplexer connection = Substitute.For<IConnectionMultiplexer>();
        IDatabase database = Substitute.For<IDatabase>();
        connection.GetDatabase().Returns(database);
        database.PingAsync(Arg.Any<CommandFlags>()).Returns(TimeSpan.FromMilliseconds(250));

        RedisHealthCheck sut = new(connection, TimeSpan.FromMilliseconds(100));

        // Act
        HealthCheckResult result = await sut.CheckHealthAsync(BuildContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("250");
        result.Description!.ShouldContain("100");
    }

    [Fact]
    public async Task CheckHealthAsync_ExceptionThrown_ReturnsUnhealthyWithSanitizedMessage()
    {
        // Arrange
        IConnectionMultiplexer connection = Substitute.For<IConnectionMultiplexer>();
        IDatabase database = Substitute.For<IDatabase>();
        connection.GetDatabase().Returns(database);
        database.PingAsync(Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect,
                "No connection available to service this operation: PING; password=secret; endpoint=redis:6379"));

        RedisHealthCheck sut = new(connection, TimeSpan.FromMilliseconds(100));

        // Act
        HealthCheckResult result = await sut.CheckHealthAsync(BuildContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Redis unreachable: RedisConnectionException");
        // Connection string and credentials must not appear
        result.Description!.ShouldNotContain("password");
        result.Description!.ShouldNotContain("redis:6379");
    }
}
