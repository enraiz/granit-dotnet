// =============================================================================
// Tests - TransitEncryptionService
// =============================================================================
// Verifies that Transit encryption/decryption works correctly.
// Vault calls are mocked via NSubstitute.
// =============================================================================

using System.Text;
using DigitalDynamics.Foundation.Vault.Options;
using DigitalDynamics.Foundation.Vault.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using VaultSharp;
using VaultSharp.V1;
using VaultSharp.V1.SecretsEngines;
using VaultSharp.V1.SecretsEngines.Transit;
using Xunit;

namespace DigitalDynamics.Foundation.Vault.Tests;

public sealed class TransitEncryptionServiceTests
{
    private readonly IVaultClient _vaultClient;
    private readonly TransitEncryptionService _sut;

    public TransitEncryptionServiceTests()
    {
        _vaultClient = Substitute.For<IVaultClient>();
        var options = Microsoft.Extensions.Options.Options.Create(new VaultOptions
        {
            TransitMountPoint = "transit"
        });
        _sut = new TransitEncryptionService(
            _vaultClient,
            options,
            NullLogger<TransitEncryptionService>.Instance);
    }

    [Fact]
    public async Task EncryptAsync_CallsVaultTransitWithBase64Plaintext()
    {
        // Arrange
        var plaintext = "donnée de santé sensible";
        var expectedBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
        var ciphertext = "vault:v1:abc123encrypted";

        var transitEngine = Substitute.For<ITransitSecretsEngine>();
        var secretsEngine = Substitute.For<ISecretsEngine>();
        secretsEngine.Transit.Returns(transitEngine);
        _vaultClient.V1.Returns(Substitute.For<IVaultClientV1>());
        _vaultClient.V1.Secrets.Returns(secretsEngine);

        transitEngine.EncryptAsync(
                "fhir-data",
                Arg.Is<EncryptRequestOptions>(r => r.Base64EncodedPlainText == expectedBase64),
                "transit")
            .Returns(new VaultSharp.V1.Commons.Secret<EncryptionResponse>
            {
                Data = new EncryptionResponse { CipherText = ciphertext }
            });

        // Act
        var result = await _sut.EncryptAsync("fhir-data", plaintext, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(ciphertext);
    }

    [Fact]
    public async Task DecryptAsync_ReturnsDecodedPlaintext()
    {
        // Arrange
        var originalText = "donnée de santé sensible";
        var base64Plaintext = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));
        var ciphertext = "vault:v1:abc123encrypted";

        var transitEngine = Substitute.For<ITransitSecretsEngine>();
        var secretsEngine = Substitute.For<ISecretsEngine>();
        secretsEngine.Transit.Returns(transitEngine);
        _vaultClient.V1.Returns(Substitute.For<IVaultClientV1>());
        _vaultClient.V1.Secrets.Returns(secretsEngine);

        transitEngine.DecryptAsync(
                "fhir-data",
                Arg.Any<DecryptRequestOptions>(),
                "transit")
            .Returns(new VaultSharp.V1.Commons.Secret<DecryptionResponse>
            {
                Data = new DecryptionResponse { Base64EncodedPlainText = base64Plaintext }
            });

        // Act
        var result = await _sut.DecryptAsync("fhir-data", ciphertext, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(originalText);
    }
}
