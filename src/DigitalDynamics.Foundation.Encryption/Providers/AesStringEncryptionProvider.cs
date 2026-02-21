// =============================================================================
// AesStringEncryptionProvider - Chiffrement AES-256-CBC avec PBKDF2
// =============================================================================
// Implémente IStringEncryptionProvider via AES-256-CBC + PBKDF2 (SHA-256).
//
// Sécurité HDS (CWE-329) :
//   - IV de 16 octets généré aléatoirement à chaque chiffrement
//   - Clé dérivée UNE SEULE FOIS au démarrage (PBKDF2 + sel fixe interne)
//   - Format de sortie : Base64(IV[16] || CipherText)
//   - La PassPhrase DOIT provenir de Vault — jamais hardcodée
//
// Inputs  : plainText (string), configuration via IOptions<StringEncryptionOptions>
// Outputs : Base64 string (encrypt) | plainText | null si erreur (decrypt)
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using DigitalDynamics.Foundation.Encryption;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Encryption.Providers;

/// <summary>
/// Provider AES-256-CBC avec dérivation de clé PBKDF2 (Rfc2898DeriveBytes/SHA-256).
/// Conçu pour les opérations fréquentes (&lt; 1 ms après démarrage).
/// </summary>
public sealed class AesStringEncryptionProvider : IStringEncryptionProvider
{
    // Sel interne fixe pour la dérivation de clé PBKDF2.
    // Acceptable ici : la PassPhrase provient de Vault (haute entropie).
    // Le sel protège contre les attaques sur des PassPhrases faibles.
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
                "Encryption:PassPhrase est requis pour AesStringEncryptionProvider. " +
                "Configurer via Vault config provider (jamais en clair dans appsettings).");
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

        using Aes aes = Aes.Create();
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

        using Aes aes = Aes.Create();
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
