// =============================================================================
// AesStringEncryptionProviderTests - Tests unitaires AES-256-CBC
// =============================================================================

using Granit.Encryption;
using Granit.Encryption.Providers;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Encryption.Tests;

public sealed class AesStringEncryptionProviderTests
{
    private static AesStringEncryptionProvider CreateProvider(string passPhrase = "P@ssw0rdVaultSecret!HDS2026")
    {
        IOptions<StringEncryptionOptions> options = Options.Create(new StringEncryptionOptions
        {
            PassPhrase = passPhrase,
            KeySize = 256,
            ProviderName = StringEncryptionOptions.AesProviderName
        });

        return new AesStringEncryptionProvider(options);
    }

    [Fact]
    public void ProviderName_Returns_Aes()
    {
        AesStringEncryptionProvider provider = CreateProvider();

        provider.ProviderName.ShouldBe("Aes");
    }

    [Theory]
    [InlineData("Bonjour monde !")]
    [InlineData("données de santé sensibles")]
    [InlineData("")]
    [InlineData("caractères Unicode : éàü €™©")]
    public void Encrypt_Decrypt_RoundTrip_Returns_Original(string plainText)
    {
        AesStringEncryptionProvider provider = CreateProvider();

        string cipherText = provider.Encrypt(plainText);
        string? decrypted = provider.Decrypt(cipherText);

        decrypted.ShouldBe(plainText);
    }

    [Fact]
    public void Encrypt_SameInput_Produces_DifferentCipherTexts()
    {
        // CWE-329 : chaque chiffrement doit produire un IV différent.
        AesStringEncryptionProvider provider = CreateProvider();
        string plainText = "texte identique";

        string cipher1 = provider.Encrypt(plainText);
        string cipher2 = provider.Encrypt(plainText);

        cipher1.ShouldNotBe(cipher2, "l'IV aléatoire doit produire des ciphertexts distincts");
    }

    [Fact]
    public void Encrypt_Output_Is_ValidBase64()
    {
        AesStringEncryptionProvider provider = CreateProvider();

        string cipherText = provider.Encrypt("test");

        byte[] bytes = Convert.FromBase64String(cipherText);
        // IV(16) + at least one AES block(16) + HMAC-SHA256(32) = 64 bytes minimum
        bytes.Length.ShouldBeGreaterThanOrEqualTo(64, "le ciphertext doit contenir IV(16) + bloc AES + HMAC(32)");
    }

    [Fact]
    public void Decrypt_Null_Returns_Null()
    {
        AesStringEncryptionProvider provider = CreateProvider();

        string? result = provider.Decrypt(null!);

        result.ShouldBeNull();
    }

    [Fact]
    public void Decrypt_Empty_Returns_Null()
    {
        AesStringEncryptionProvider provider = CreateProvider();

        string? result = provider.Decrypt(string.Empty);

        result.ShouldBeNull();
    }

    [Fact]
    public void Decrypt_InvalidBase64_Returns_Null()
    {
        AesStringEncryptionProvider provider = CreateProvider();

        string? result = provider.Decrypt("ceci-n-est-pas-du-base64!!!");

        result.ShouldBeNull();
    }

    [Fact]
    public void Decrypt_TamperedCipherText_Returns_Null()
    {
        AesStringEncryptionProvider provider = CreateProvider();
        string cipherText = provider.Encrypt("données sensibles");

        // Altérer le ciphertext pour simuler une falsification
        byte[] bytes = Convert.FromBase64String(cipherText);
        bytes[^1] ^= 0xFF;
        string tampered = Convert.ToBase64String(bytes);

        string? result = provider.Decrypt(tampered);

        result.ShouldBeNull("un ciphertext falsifié doit échouer silencieusement");
    }

    [Fact]
    public void Decrypt_TooShortInput_Returns_Null()
    {
        AesStringEncryptionProvider provider = CreateProvider();
        // Moins de 64 octets (IV=16 + bloc AES=16 + HMAC-SHA256=32)
        string tooShort = Convert.ToBase64String(new byte[50]);

        string? result = provider.Decrypt(tooShort);

        result.ShouldBeNull();
    }

    [Fact]
    public void Constructor_EmptyPassPhrase_Throws_InvalidOperationException()
    {
        IOptions<StringEncryptionOptions> options = Options.Create(new StringEncryptionOptions
        {
            PassPhrase = string.Empty
        });

        Action act = () => _ = new AesStringEncryptionProvider(options);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("PassPhrase");
    }

    [Fact]
    public void Decrypt_WrongKey_Returns_Null()
    {
        AesStringEncryptionProvider providerA = CreateProvider("clé-A-vault-secret");
        AesStringEncryptionProvider providerB = CreateProvider("clé-B-vault-secret");

        string cipherText = providerA.Encrypt("données confidentielles");
        string? result = providerB.Decrypt(cipherText);

        result.ShouldBeNull("un ciphertext chiffré avec la clé A ne peut pas être déchiffré avec la clé B");
    }
}
