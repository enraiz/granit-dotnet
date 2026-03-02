using System.Security.Cryptography;
using System.Text;
using Granit.Encryption;
using Microsoft.Extensions.Options;

namespace Granit.Encryption.Providers;

/// <summary>
/// AES-256-CBC + HMAC-SHA256 provider with PBKDF2 key derivation (encrypt-then-MAC).
/// <para>
/// Output format: <c>Base64(IV[16] || CipherText || HMAC-SHA256(IV || CipherText)[32])</c>.
/// </para>
/// <para>
/// The HMAC tag guarantees ciphertext integrity and authenticity, preventing
/// padding oracle attacks and silent data corruption (HDS requirement).
/// </para>
/// Designed for frequent operations (&lt; 1 ms after startup).
/// </summary>
public sealed class AesStringEncryptionProvider : IStringEncryptionProvider
{
    /// <remarks>
    /// SECURITY: Fixed internal salt for PBKDF2 key derivation.
    /// This is acceptable because the PassPhrase MUST come from Vault (high entropy, 256-bit minimum).
    /// The salt's purpose is to prevent rainbow table attacks on weak PassPhrases;
    /// with a Vault-sourced PassPhrase, the fixed salt does not degrade security.
    /// Changing this salt would invalidate all previously encrypted data — do NOT modify.
    /// </remarks>
    private static readonly byte[] KeyDerivationSalt =
    [
        0x44, 0x44, 0x46, 0x6F, 0x75, 0x6E, 0x64, 0x61,
        0x74, 0x69, 0x6F, 0x6E, 0x45, 0x6E, 0x63, 0x72
    ];

    private const int KeyDerivationIterations = 100_000;
    private const int IvSize = 16;
    private const int HmacSize = 32; // HMAC-SHA256

    private readonly byte[] _aesKey;
    private readonly byte[] _hmacKey;

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

        int aesKeySize = opts.KeySize / 8;
        byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            opts.PassPhrase,
            KeyDerivationSalt,
            KeyDerivationIterations,
            HashAlgorithmName.SHA256,
            aesKeySize + HmacSize);

        _aesKey = derivedKey[..aesKeySize];
        _hmacKey = derivedKey[aesKeySize..];
    }

    /// <inheritdoc/>
    public string Encrypt(string plainText)
    {
        byte[] iv = RandomNumberGenerator.GetBytes(IvSize);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        using Aes aes = Aes.Create();
        aes.Key = _aesKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Encrypt-then-MAC : HMAC-SHA256(IV || CipherText)
        int dataLength = IvSize + cipherBytes.Length;
        byte[] dataToMac = new byte[dataLength];
        Buffer.BlockCopy(iv, 0, dataToMac, 0, IvSize);
        Buffer.BlockCopy(cipherBytes, 0, dataToMac, IvSize, cipherBytes.Length);
        byte[] hmac = HMACSHA256.HashData(_hmacKey, dataToMac);

        // Format : IV[16] || CipherText || HMAC[32]
        byte[] output = new byte[dataLength + HmacSize];
        Buffer.BlockCopy(dataToMac, 0, output, 0, dataLength);
        Buffer.BlockCopy(hmac, 0, output, dataLength, HmacSize);

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

        // Minimum: IV[16] + one AES block[16] + HMAC[32] = 64 bytes
        if (input.Length < IvSize + 16 + HmacSize)
        {
            return null;
        }

        // Verify HMAC before decryption (encrypt-then-MAC: always verify first)
        byte[] receivedHmac = input[^HmacSize..];
        byte[] dataToMac = input[..^HmacSize];
        byte[] computedHmac = HMACSHA256.HashData(_hmacKey, dataToMac);

        if (!CryptographicOperations.FixedTimeEquals(computedHmac, receivedHmac))
        {
            return null;
        }

        byte[] iv = input[..IvSize];
        byte[] cipherBytes = input[IvSize..^HmacSize];

        using Aes aes = Aes.Create();
        aes.Key = _aesKey;
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
