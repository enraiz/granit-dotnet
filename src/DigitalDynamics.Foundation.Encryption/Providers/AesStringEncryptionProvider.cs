using System.Security.Cryptography;
using System.Text;
using DigitalDynamics.Foundation.Encryption;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Encryption.Providers;

/// <summary>
/// AES-256-CBC provider with PBKDF2 key derivation (Rfc2898DeriveBytes/SHA-256).
/// Designed for frequent operations (&lt; 1 ms after startup).
/// </summary>
public sealed class AesStringEncryptionProvider : IStringEncryptionProvider
{
    // Fixed internal salt for PBKDF2 key derivation.
    // Acceptable here: the PassPhrase comes from Vault (high entropy).
    // The salt protects against attacks on weak PassPhrases.
    private static readonly byte[] KeyDerivationSalt =
    [
        0x44, 0x44, 0x46, 0x6F, 0x75, 0x6E, 0x64, 0x61,
        0x74, 0x69, 0x6F, 0x6E, 0x45, 0x6E, 0x63, 0x72
    ];

    private const int KeyDerivationIterations = 10_000;
    private const int IvSize = 16;

    private readonly byte[] _key;

    /// <inheritdoc/>
    public string ProviderName => StringEncryptionOptions.AesProviderName;

    public AesStringEncryptionProvider(IOptions<StringEncryptionOptions> options)
    {
        StringEncryptionOptions opts = options.Value;

        if (string.IsNullOrEmpty(opts.PassPhrase))
        {
            throw new InvalidOperationException(
                "Encryption:PassPhrase is required for AesStringEncryptionProvider. " +
                "Configure via Vault config provider (never in plain text in appsettings).");
        }

        _key = Rfc2898DeriveBytes.Pbkdf2(
            opts.PassPhrase,
            KeyDerivationSalt,
            KeyDerivationIterations,
            HashAlgorithmName.SHA256,
            opts.KeySize / 8);
    }

    /// <inheritdoc/>
    public string Encrypt(string plainText)
    {
        byte[] iv = RandomNumberGenerator.GetBytes(IvSize);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Format : IV[16] || CipherText
        byte[] output = new byte[IvSize + cipherBytes.Length];
        Buffer.BlockCopy(iv, 0, output, 0, IvSize);
        Buffer.BlockCopy(cipherBytes, 0, output, IvSize, cipherBytes.Length);

        return Convert.ToBase64String(output);
    }

    /// <inheritdoc/>
    public string? Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return null;
        }

        byte[] input;
        try
        {
            input = Convert.FromBase64String(cipherText);
        }
        catch (FormatException)
        {
            return null;
        }

        if (input.Length < IvSize + 1)
        {
            return null;
        }

        byte[] iv = input[..IvSize];
        byte[] cipherBytes = input[IvSize..];

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        try
        {
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
