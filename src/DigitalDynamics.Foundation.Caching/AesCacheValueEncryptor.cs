using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Caching;

/// <summary>
/// Implémentation AES-256-CBC de <see cref="ICacheValueEncryptor"/> pour la conformité HDS.
/// </summary>
/// <remarks>
/// Caractéristiques de sécurité :
/// <list type="bullet">
///   <item>Algorithme : AES-256-CBC via <see cref="Aes.Create()"/></item>
///   <item>IV : 16 bytes générés aléatoirement par <see cref="RandomNumberGenerator.Fill"/> pour chaque opération <see cref="Encrypt"/></item>
///   <item>Format ciphertext : <c>[16 bytes IV][N bytes CipherText]</c></item>
///   <item>Clé : 256 bits (32 bytes) fournie en base64 via <c>Cache:Encryption:Key</c></item>
/// </list>
/// La clé AES doit être fournie exclusivement via Vault / configuration sécurisée.
/// Ne jamais stocker la clé en clair dans le code ou les fichiers de configuration commités.
/// </remarks>
public sealed class AesCacheValueEncryptor : ICacheValueEncryptor
{
    private const int IvSizeBytes = 16;
    private const int KeySizeBits = 256;
    private const int KeySizeBytes = KeySizeBits / 8;

    private readonly byte[] _key;

    /// <param name="options">Options de chiffrement contenant la clé AES en base64.</param>
    /// <exception cref="InvalidOperationException">Si la clé est absente de la configuration.</exception>
    /// <exception cref="ArgumentException">Si la clé n'est pas de 256 bits (32 bytes).</exception>
    public AesCacheValueEncryptor(IOptions<CacheEncryptionOptions> options)
    {
        string? base64Key = options.Value.Key;

        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new InvalidOperationException(
                "La clé AES (Cache:Encryption:Key) est requise pour le chiffrement du cache. " +
                "Fournissez-la via Vault ou les variables d'environnement.");
        }

        _key = Convert.FromBase64String(base64Key);

        if (_key.Length != KeySizeBytes)
        {
            throw new ArgumentException(
                $"La clé AES doit être de {KeySizeBits} bits ({KeySizeBytes} bytes). " +
                $"Longueur reçue : {_key.Length * 8} bits ({_key.Length} bytes).");
        }
    }

    /// <summary>
    /// Chiffre les données en AES-256-CBC avec un IV aléatoire.
    /// </summary>
    /// <param name="plaintext">Données en clair.</param>
    /// <returns>Format <c>[16 bytes IV][N bytes CipherText]</c>.</returns>
    public byte[] Encrypt(byte[] plaintext)
    {
        byte[] iv = new byte[IvSizeBytes];
        RandomNumberGenerator.Fill(iv);

        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] result = new byte[IvSizeBytes + ciphertext.Length];
        Buffer.BlockCopy(iv, 0, result, 0, IvSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, result, IvSizeBytes, ciphertext.Length);

        return result;
    }

    /// <summary>
    /// Déchiffre les données au format <c>[16 bytes IV][N bytes CipherText]</c>.
    /// </summary>
    /// <param name="ciphertext">Données chiffrées.</param>
    /// <returns>Données déchiffrées.</returns>
    /// <exception cref="ArgumentException">Si le ciphertext est trop court (moins de 16 bytes).</exception>
    public byte[] Decrypt(byte[] ciphertext)
    {
        if (ciphertext.Length < IvSizeBytes)
        {
            throw new ArgumentException(
                $"Le ciphertext est invalide : minimum {IvSizeBytes} bytes requis (IV), " +
                $"reçu {ciphertext.Length} bytes.");
        }

        byte[] iv = new byte[IvSizeBytes];
        Buffer.BlockCopy(ciphertext, 0, iv, 0, IvSizeBytes);

        int cipherLength = ciphertext.Length - IvSizeBytes;
        byte[] cipher = new byte[cipherLength];
        Buffer.BlockCopy(ciphertext, IvSizeBytes, cipher, 0, cipherLength);

        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using ICryptoTransform decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
    }
}
