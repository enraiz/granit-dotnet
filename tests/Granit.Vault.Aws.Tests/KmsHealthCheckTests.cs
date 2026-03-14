using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Granit.Vault.Aws.HealthChecks;
using Granit.Vault.Aws.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Granit.Vault.Aws.Tests;

public sealed class KmsHealthCheckTests
{
    private readonly IAmazonKeyManagementService _kmsClient = Substitute.For<IAmazonKeyManagementService>();
    private readonly KmsHealthCheck _sut;

    public KmsHealthCheckTests()
    {
        AwsVaultOptions options = new()
        {
            Region = "eu-west-1",
            KmsKeyId = "alias/test-key",
        };

        _sut = new KmsHealthCheck(
            _kmsClient,
            Microsoft.Extensions.Options.Options.Create(options));
    }

    private static HealthCheckContext CreateContext() =>
        new() { Registration = new HealthCheckRegistration("test", _ => null!, null, null) };

    [Fact]
    public async Task CheckHealthAsync_EnabledKey_ReturnsHealthy()
    {
        _kmsClient.DescribeKeyAsync(Arg.Any<DescribeKeyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DescribeKeyResponse
            {
                KeyMetadata = new KeyMetadata { Enabled = true },
            });

        HealthCheckResult result = await _sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_DisabledKey_ReturnsUnhealthy()
    {
        _kmsClient.DescribeKeyAsync(Arg.Any<DescribeKeyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DescribeKeyResponse
            {
                KeyMetadata = new KeyMetadata { Enabled = false },
            });

        HealthCheckResult result = await _sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("disabled");
    }

    [Fact]
    public async Task CheckHealthAsync_Exception_ReturnsUnhealthy()
    {
        _kmsClient.DescribeKeyAsync(Arg.Any<DescribeKeyRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonKeyManagementServiceException("timeout"));

        HealthCheckResult result = await _sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("KMS unreachable");
    }

    [Fact]
    public async Task CheckHealthAsync_SendsCorrectKeyId()
    {
        _kmsClient.DescribeKeyAsync(Arg.Any<DescribeKeyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DescribeKeyResponse
            {
                KeyMetadata = new KeyMetadata { Enabled = true },
            });

        DescribeKeyRequest? captured = null;
        await _kmsClient.DescribeKeyAsync(
            Arg.Do<DescribeKeyRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        await _sut.CheckHealthAsync(CreateContext(), TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.KeyId.ShouldBe("alias/test-key");
    }
}
