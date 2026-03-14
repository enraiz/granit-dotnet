using Google.Cloud.Kms.V1;
using Granit.Vault.GoogleCloud.HealthChecks;
using Granit.Vault.GoogleCloud.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Granit.Vault.GoogleCloud.Tests;

public sealed class CloudKmsHealthCheckTests
{
    private readonly KeyManagementServiceClient _kmsClient = Substitute.For<KeyManagementServiceClient>();
    private readonly CloudKmsHealthCheck _sut;

    public CloudKmsHealthCheckTests()
    {
        GoogleCloudVaultOptions options = new()
        {
            ProjectId = "my-project",
            Location = "europe-west1",
            KeyRing = "granit-keyring",
            CryptoKey = "granit-key",
        };

        _sut = new CloudKmsHealthCheck(
            _kmsClient,
            Microsoft.Extensions.Options.Options.Create(options));
    }

    [Fact]
    public async Task CheckHealthAsync_EnabledKey_ReturnsHealthy()
    {
        CryptoKey key = new()
        {
            Primary = new CryptoKeyVersion
            {
                State = CryptoKeyVersion.Types.CryptoKeyVersionState.Enabled,
            },
        };

        _kmsClient.GetCryptoKeyAsync(
                Arg.Any<CryptoKeyName>(),
                Arg.Any<CancellationToken>())
            .Returns(key);

        HealthCheckResult result = await _sut.CheckHealthAsync(
            CreateContext(),
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_DisabledKey_ReturnsUnhealthy()
    {
        CryptoKey key = new()
        {
            Primary = new CryptoKeyVersion
            {
                State = CryptoKeyVersion.Types.CryptoKeyVersionState.Disabled,
            },
        };

        _kmsClient.GetCryptoKeyAsync(
                Arg.Any<CryptoKeyName>(),
                Arg.Any<CancellationToken>())
            .Returns(key);

        HealthCheckResult result = await _sut.CheckHealthAsync(
            CreateContext(),
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_Exception_ReturnsUnhealthy()
    {
        _kmsClient.GetCryptoKeyAsync(
                Arg.Any<CryptoKeyName>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Unavailable, "unavailable")));

        HealthCheckResult result = await _sut.CheckHealthAsync(
            CreateContext(),
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    private static HealthCheckContext CreateContext() =>
        new()
        {
            Registration = new HealthCheckRegistration(
                "gcp-kms",
                Substitute.For<IHealthCheck>(),
                null,
                null),
        };
}
