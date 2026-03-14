using System.Text;
using Granit.Vault.HashiCorp.Options;
using Granit.Vault.HashiCorp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using VaultSharp;
using VaultSharp.V1;
using VaultSharp.V1.SecretsEngines;
using VaultSharp.V1.SecretsEngines.Transit;
using Xunit;

namespace Granit.Vault.HashiCorp.Tests;

public sealed class HashiCorpTransitEncryptionServiceTests
{
    private readonly IVaultClient _vaultClient;
    private readonly HashiCorpTransitEncryptionService _sut;

    public HashiCorpTransitEncryptionServiceTests()
    {
        _vaultClient = Substitute.For<IVaultClient>();
        IOptions<HashiCorpVaultOptions> vaultOptions = Microsoft.Extensions.Options.Options.Create(new HashiCorpVaultOptions
        {
            TransitMountPoint = "transit"
        });
        _sut = new HashiCorpTransitEncryptionService(
            _vaultClient,
            vaultOptions,
            NullLogger<HashiCorpTransitEncryptionService>.Instance);
    }

    [Fact]
    public async Task EncryptAsync_CallsVaultTransitWithBase64Plaintext()
    {
        string plaintext = "donnée de santé sensible";
        string expectedBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
        string ciphertext = "vault:v1:abc123encrypted";

        ITransitSecretsEngine transitEngine = Substitute.For<ITransitSecretsEngine>();
        ISecretsEngine secretsEngine = Substitute.For<ISecretsEngine>();
        secretsEngine.Transit.Returns(transitEngine);
        _vaultClient.V1.Returns(Substitute.For<IVaultClientV1>());
        _vaultClient.V1.Secrets.Returns(secretsEngine);

        transitEngine.EncryptAsync(
                "sensitive-data",
                Arg.Is<EncryptRequestOptions>(r => r.Base64EncodedPlainText == expectedBase64),
                "transit")
            .Returns(new VaultSharp.V1.Commons.Secret<EncryptionResponse>
            {
                Data = new EncryptionResponse { CipherText = ciphertext }
            });

        string result = await _sut.EncryptAsync("sensitive-data", plaintext, TestContext.Current.CancellationToken);

        result.ShouldBe(ciphertext);
    }

    [Fact]
    public async Task DecryptAsync_ReturnsDecodedPlaintext()
    {
        string originalText = "donnée personnelle sensible";
        string base64Plaintext = Convert.ToBase64String(Encoding.UTF8.GetBytes(originalText));
        string ciphertext = "vault:v1:abc123encrypted";

        ITransitSecretsEngine transitEngine = Substitute.For<ITransitSecretsEngine>();
        ISecretsEngine secretsEngine = Substitute.For<ISecretsEngine>();
        secretsEngine.Transit.Returns(transitEngine);
        _vaultClient.V1.Returns(Substitute.For<IVaultClientV1>());
        _vaultClient.V1.Secrets.Returns(secretsEngine);

        transitEngine.DecryptAsync(
                "sensitive-data",
                Arg.Any<DecryptRequestOptions>(),
                "transit")
            .Returns(new VaultSharp.V1.Commons.Secret<DecryptionResponse>
            {
                Data = new DecryptionResponse { Base64EncodedPlainText = base64Plaintext }
            });

        string result = await _sut.DecryptAsync("sensitive-data", ciphertext, TestContext.Current.CancellationToken);

        result.ShouldBe(originalText);
    }
}
