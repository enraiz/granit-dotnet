using Azure;
using Azure.Security.KeyVault.Keys;
using Granit.Vault.Azure.HealthChecks;
using Granit.Vault.Azure.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Granit.Vault.Azure.Tests;

public sealed class AzureKeyVaultHealthCheckTests
{
    private readonly KeyClient _keyClient = Substitute.For<KeyClient>();
    private readonly AzureKeyVaultHealthCheck _sut;

    public AzureKeyVaultHealthCheckTests()
    {
        AzureKeyVaultOptions options = new()
        {
            VaultUri = "https://my-vault.vault.azure.net/",
            EncryptionKeyName = "test-key",
        };

        _sut = new AzureKeyVaultHealthCheck(
            _keyClient,
            Microsoft.Extensions.Options.Options.Create(options));
    }

    private static HealthCheckContext CreateContext() =>
        new() { Registration = new HealthCheckRegistration("test", _ => null!, null, null) };

    [Fact]
    public async Task CheckHealthAsync_EnabledKey_ReturnsHealthy()
    {
        KeyProperties keyProperties = new("test-key") { Enabled = true };
        KeyVaultKey key = KeyModelFactory.KeyVaultKey(keyProperties, new JsonWebKey([]));
        _keyClient.GetKeyAsync("test-key", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(key, Substitute.For<Response>()));

        HealthCheckResult result = await _sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_DisabledKey_ReturnsUnhealthy()
    {
        KeyProperties keyProperties = new("test-key") { Enabled = false };
        KeyVaultKey key = KeyModelFactory.KeyVaultKey(keyProperties, new JsonWebKey([]));
        _keyClient.GetKeyAsync("test-key", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(key, Substitute.For<Response>()));

        HealthCheckResult result = await _sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("disabled");
    }

    [Fact]
    public async Task CheckHealthAsync_Exception_ReturnsUnhealthy()
    {
        _keyClient.GetKeyAsync("test-key", cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException("timeout"));

        HealthCheckResult result = await _sut.CheckHealthAsync(
            CreateContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Azure Key Vault unreachable");
    }
}
