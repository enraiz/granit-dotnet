// =============================================================================
// Tests - AesCacheValueEncryptor
// =============================================================================
// Vérifie que le chiffrement/déchiffrement AES-256-CBC fonctionne correctement :
//   - Encrypt(Decrypt(x)) == x (round-trip)
//   - IV aléatoire : deux chiffrements du même plaintext donnent des ciphertexts différents
//   - Rejet d'une clé invalide (taille != 32 octets)
// =============================================================================

using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Caching.Tests;

public sealed class AesCacheValueEncryptorTests
{
    private static AesCacheValueEncryptor CreateEncryptor(string? keyBase64 = null)
    {
        if (keyBase64 is null)
        {
            byte[] key = new byte[32];
            RandomNumberGenerator.Fill(key);
            keyBase64 = Convert.ToBase64String(key);
        }

        CacheEncryptionOptions encryptionOptions = new() { Key = keyBase64 };
        return new AesCacheValueEncryptor(Options.Create(encryptionOptions));
    }

    [Fact]
    public void EncryptThenDecrypt_ReturnsOriginalPlaintext()
    {
        // Arrange
        AesCacheValueEncryptor encryptor = CreateEncryptor();
        byte[] plaintext = "données de santé sensibles"u8.ToArray();

        // Act
        byte[] ciphertext = encryptor.Encrypt(plaintext);
        byte[] decrypted = encryptor.Decrypt(ciphertext);

        // Assert
        decrypted.ShouldBe(plaintext);
    }

    [Fact]
    public void Encrypt_SamePlaintext_ProducesDifferentCiphertexts()
    {
        // Arrange — IV aléatoire par opération (Gemini: jamais de IV hardcodé)
        AesCacheValueEncryptor encryptor = CreateEncryptor();
        byte[] plaintext = "test stampede HDS"u8.ToArray();

        // Act
        byte[] cipher1 = encryptor.Encrypt(plaintext);
        byte[] cipher2 = encryptor.Encrypt(plaintext);

        // Assert
        cipher1.ShouldNotBe(cipher2, "l'IV aléatoire doit produire des ciphertexts distincts");
    }

    [Fact]
    public void Decrypt_CiphertextPrefixedWithIv_ExtractsCorrectIv()
    {
        // Arrange — format : [16 octets IV][N octets CipherText]
        AesCacheValueEncryptor encryptor = CreateEncryptor();
        byte[] plaintext = "vérification format IV"u8.ToArray();

        // Act
        byte[] ciphertext = encryptor.Encrypt(plaintext);

        // Assert — le ciphertext doit être plus long que l'IV seul (16 octets)
        ciphertext.Length.ShouldBeGreaterThan(16);
    }

    [Fact]
    public void Constructor_KeyNot32Bytes_ThrowsArgumentException()
    {
        // Arrange — clé AES invalide (16 octets au lieu de 32)
        byte[] shortKey = new byte[16];
        RandomNumberGenerator.Fill(shortKey);
        CacheEncryptionOptions opts = new() { Key = Convert.ToBase64String(shortKey) };

        // Act
        Action act = () => _ = new AesCacheValueEncryptor(Options.Create(opts));

        // Assert
        Should.Throw<ArgumentException>(act).Message.ShouldContain("256");
    }

    [Fact]
    public void Constructor_NullKey_ThrowsInvalidOperationException()
    {
        // Arrange
        CacheEncryptionOptions opts = new() { Key = null };

        // Act
        Action act = () => _ = new AesCacheValueEncryptor(Options.Create(opts));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void EncryptThenDecrypt_EmptyByteArray_ReturnsEmpty()
    {
        // Arrange
        AesCacheValueEncryptor encryptor = CreateEncryptor();
        byte[] plaintext = [];

        // Act
        byte[] ciphertext = encryptor.Encrypt(plaintext);
        byte[] decrypted = encryptor.Decrypt(ciphertext);

        // Assert
        decrypted.ShouldBeEmpty();
    }

    [Fact]
    public void Decrypt_CiphertextShorterThan16Bytes_ThrowsArgumentException()
    {
        // Arrange — ciphertext invalide : moins de 16 octets (taille de l'IV)
        AesCacheValueEncryptor encryptor = CreateEncryptor();
        byte[] tooShort = new byte[10];

        // Act
        Action act = () => encryptor.Decrypt(tooShort);

        // Assert
        Should.Throw<ArgumentException>(act).Message.ShouldContain("16");
    }
}
