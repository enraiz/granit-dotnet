// =============================================================================
// Tests - TransitEncryptionService
// =============================================================================
// Verifies that Transit encryption/decryption works correctly.
// Vault calls are mocked via NSubstitute.
// =============================================================================

using System.Text;
using Granit.Vault.Options;
using Granit.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using VaultSharp;
using VaultSharp.V1;
using VaultSharp.V1.SecretsEngines;
using VaultSharp.V1.SecretsEngines.Transit;
using Xunit;

namespace Granit.Vault.Tests;

public sealed class TransitEncryptionServiceTests
{
    private readonly IVaultClient _vaultClient;
    private readonly TransitEncryptionService _sut;

    public TransitEncryptionServiceTests()
    {
        _vaultClient = Substitute.For<IVaultClient>();
        IOptions<VaultOptions> vaultOptions = Microsoft.Extensions.Options.Options.Create(new VaultOptions
        {
            TransitMountPoint = "transit"
        });
        _sut = new TransitEncryptionService(
            _vaultClient,
            vaultOptions,
            NullLogger<TransitEncryptionService>.Instance);
    }

    [Fact]
    public async Task EncryptAsync_CallsVaultTransitWithBase64Plaintext()
    {
        // Arrange
        string plaintext = "donnée de santé sensible";
        string expectedBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
        string ciphertext = "vault:v1:abc123encrypted";

        ITransitSecretsEngine transitEngine = Substitute.For<ITransitSecretsEngine>();
        ISecretsEngine secretsEngine = Substitute.For<ISecretsEngine>();
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
        string result = await _sut.EncryptAsync("fhir-data", plaintext, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(ciphertext);
    }

    [Fact]
    public async Task DecryptAsync_ReturnsDecodedPlaintext()
    {
        // Arrange
        string originalText = "donnée de santé sensible";
        string base64Plaintext = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));
        string ciphertext = "vault:v1:abc123encrypted";

        ITransitSecretsEngine transitEngine = Substitute.For<ITransitSecretsEngine>();
        ISecretsEngine secretsEngine = Substitute.For<ISecretsEngine>();
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
        string result = await _sut.DecryptAsync("fhir-data", ciphertext, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(originalText);
    }
}
