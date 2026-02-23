namespace Granit.Caching;

/// <summary>
/// Chiffre et déchiffre les valeurs sérialisées avant leur stockage dans <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>.
/// </summary>
/// <remarks>
/// Deux implémentations fournies :
/// <list type="bullet">
///   <item><see cref="NullCacheValueEncryptor"/> — no-op, utilisé en développement avec le fournisseur Memory.</item>
///   <item><see cref="AesCacheValueEncryptor"/> — AES-256-CBC avec IV aléatoire, utilisé en production avec Redis.</item>
/// </list>
/// Le chiffrement est activé par type via <see cref="CacheEncryptedAttribute"/> ou globalement via
/// <see cref="CachingOptions.EncryptValues"/>.
/// </remarks>
public interface ICacheValueEncryptor
{
    /// <summary>
    /// Chiffre les octets fournis. Génère un IV aléatoire par appel (AES-256-CBC).
    /// </summary>
    /// <param name="plaintext">Données en clair (JSON sérialisé).</param>
    /// <returns>Données chiffrées au format <c>[16 bytes IV][N bytes CipherText]</c>.</returns>
    byte[] Encrypt(byte[] plaintext);

    /// <summary>
    /// Déchiffre les octets fournis au format <c>[16 bytes IV][N bytes CipherText]</c>.
    /// </summary>
    /// <param name="ciphertext">Données chiffrées.</param>
    /// <returns>Données déchiffrées (JSON sérialisé).</returns>
    byte[] Decrypt(byte[] ciphertext);
}
