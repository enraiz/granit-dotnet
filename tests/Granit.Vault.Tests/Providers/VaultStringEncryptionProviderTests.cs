using Granit.Encryption;
using Granit.Vault.Providers;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Granit.Vault.Tests.Providers;

public sealed class VaultStringEncryptionProviderTests
{
    private const string KeyName = "test-key";

    private static VaultStringEncryptionProvider CreateProvider(
        ITransitEncryptionService? transitEncryption = null)
    {
        ITransitEncryptionService service = transitEncryption ?? Substitute.For<ITransitEncryptionService>();
        IOptions<StringEncryptionOptions> options = Microsoft.Extensions.Options.Options.Create(new StringEncryptionOptions
        {
            VaultKeyName = KeyName,
        });
        return new VaultStringEncryptionProvider(service, options);
    }

    // -------------------------------------------------------------------------
    // ProviderName
    // -------------------------------------------------------------------------

    [Fact]
    public void ProviderName_Returns_VaultProviderName()
    {
        VaultStringEncryptionProvider provider = CreateProvider();

        provider.ProviderName.ShouldBe(StringEncryptionOptions.VaultProviderName);
    }

    // -------------------------------------------------------------------------
    // Encrypt
    // -------------------------------------------------------------------------

    [Fact]
    public void Encrypt_CallsEncryptAsyncWithKeyName()
    {
        ITransitEncryptionService service = Substitute.For<ITransitEncryptionService>();
        service.EncryptAsync(KeyName, "plain", Arg.Any<CancellationToken>()).Returns("vault:v1:encrypted");
        VaultStringEncryptionProvider provider = CreateProvider(service);

        string result = provider.Encrypt("plain");

        result.ShouldBe("vault:v1:encrypted");
        service.Received(1).EncryptAsync(KeyName, "plain", Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Decrypt
    // -------------------------------------------------------------------------

    [Fact]
    public void Decrypt_CallsDecryptAsyncWithKeyName()
    {
        ITransitEncryptionService service = Substitute.For<ITransitEncryptionService>();
        service.DecryptAsync(KeyName, "vault:v1:encrypted", Arg.Any<CancellationToken>()).Returns("plain");
        VaultStringEncryptionProvider provider = CreateProvider(service);

        string? result = provider.Decrypt("vault:v1:encrypted");

        result.ShouldBe("plain");
        service.Received(1).DecryptAsync(KeyName, "vault:v1:encrypted", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Decrypt_NullOrEmpty_ReturnsNull(string? cipherText)
    {
        ITransitEncryptionService service = Substitute.For<ITransitEncryptionService>();
        VaultStringEncryptionProvider provider = CreateProvider(service);

        string? result = provider.Decrypt(cipherText!);

        result.ShouldBeNull();
        service.DidNotReceive().DecryptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Decrypt_ServiceThrows_ReturnsNull()
    {
        ITransitEncryptionService service = Substitute.For<ITransitEncryptionService>();
        service.DecryptAsync(KeyName, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Vault unavailable"));
        VaultStringEncryptionProvider provider = CreateProvider(service);

        string? result = provider.Decrypt("vault:v1:corrupted");

        result.ShouldBeNull("exceptions must be silenced — return null instead of propagating");
    }
}
