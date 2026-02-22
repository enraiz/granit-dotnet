namespace DigitalDynamics.Foundation.Caching;

/// <summary>
/// Implémentation no-op de <see cref="ICacheValueEncryptor"/>.
/// Retourne les données en entrée sans modification.
/// Utilisée par défaut avec le fournisseur Memory (développement, tests).
/// </summary>
public sealed class NullCacheValueEncryptor : ICacheValueEncryptor
{
    /// <inheritdoc/>
    public byte[] Encrypt(byte[] plaintext) => plaintext;

    /// <inheritdoc/>
    public byte[] Decrypt(byte[] ciphertext) => ciphertext;
}
