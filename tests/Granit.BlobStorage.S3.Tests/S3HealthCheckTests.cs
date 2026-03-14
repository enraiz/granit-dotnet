using Granit.BlobStorage.S3.HealthChecks;
using Granit.BlobStorage.S3.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.S3.Tests;

/// <summary>
/// Unit tests for <see cref="S3HealthCheck"/>.
/// Only tests the exception path — happy-path requires a real S3-compatible endpoint.
/// </summary>
public sealed class S3HealthCheckTests
{
    private static HealthCheckContext CreateContext() =>
        new() { Registration = new HealthCheckRegistration("test", _ => null!, null, null) };

    [Fact]
    public async Task CheckHealthAsync_UnreachableEndpoint_ReturnsUnhealthyWithSanitizedMessage()
    {
        // Arrange — point to a non-existent S3 endpoint
        S3BlobOptions options = new()
        {
            ServiceUrl = "http://127.0.0.1:1",
            AccessKey = "test-key",
            SecretKey = "test-secret",
            DefaultBucket = "test-bucket",
            Region = "us-east-1",
            ForcePathStyle = true,
        };

        S3HealthCheck sut = new(Microsoft.Extensions.Options.Options.Create(options));

        // Act
        HealthCheckResult result = await sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldStartWith("S3 unreachable:");
        // Must not expose endpoint URL or credentials
        result.Description!.ShouldNotContain("127.0.0.1");
        result.Description!.ShouldNotContain("test-key");
        result.Description!.ShouldNotContain("test-secret");
        result.Description!.ShouldNotContain("test-bucket");
    }
}
