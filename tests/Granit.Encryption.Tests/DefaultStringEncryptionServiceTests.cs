// =============================================================================
// DefaultStringEncryptionServiceTests - Tests du service principal
// =============================================================================

using FluentAssertions;
using Granit.Encryption;
using Granit.Encryption.Services;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Granit.Encryption.Tests;

public sealed class DefaultStringEncryptionServiceTests
{
    private static IOptions<StringEncryptionOptions> OptionsFor(string providerName) =>
        Options.Create(new StringEncryptionOptions { ProviderName = providerName });

    [Fact]
    public void Encrypt_Delegates_To_Selected_Provider()
    {
        IStringEncryptionProvider provider = Substitute.For<IStringEncryptionProvider>();
        provider.ProviderName.Returns("Aes");
        provider.Encrypt("hello").Returns("chiffré");

        DefaultStringEncryptionService service = new(
            [provider],
            OptionsFor("Aes"));

        string result = service.Encrypt("hello");

        result.Should().Be("chiffré");
        provider.Received(1).Encrypt("hello");
    }

    [Fact]
    public void Decrypt_Delegates_To_Selected_Provider()
    {
        IStringEncryptionProvider provider = Substitute.For<IStringEncryptionProvider>();
        provider.ProviderName.Returns("Aes");
        provider.Decrypt("chiffré").Returns("hello");

        DefaultStringEncryptionService service = new(
            [provider],
            OptionsFor("Aes"));

        string? result = service.Decrypt("chiffré");

        result.Should().Be("hello");
        provider.Received(1).Decrypt("chiffré");
    }

    [Fact]
    public void Constructor_UnknownProvider_Throws_InvalidOperationException()
    {
        IStringEncryptionProvider aesProvider = Substitute.For<IStringEncryptionProvider>();
        aesProvider.ProviderName.Returns("Aes");

        Action act = () => _ = new DefaultStringEncryptionService(
            [aesProvider],
            OptionsFor("Vault"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Vault*");
    }

    [Fact]
    public void Constructor_MultipleProviders_Selects_Correct_One()
    {
        IStringEncryptionProvider aesProvider = Substitute.For<IStringEncryptionProvider>();
        aesProvider.ProviderName.Returns("Aes");

        IStringEncryptionProvider vaultProvider = Substitute.For<IStringEncryptionProvider>();
        vaultProvider.ProviderName.Returns("Vault");
        vaultProvider.Encrypt("secret").Returns("vault-chiffré");

        DefaultStringEncryptionService service = new(
            [aesProvider, vaultProvider],
            OptionsFor("Vault"));

        service.Encrypt("secret");

        vaultProvider.Received(1).Encrypt("secret");
        aesProvider.DidNotReceive().Encrypt(Arg.Any<string>());
    }
}
