namespace DigitalDynamics.Foundation.Caching;

/// <summary>
/// Options de chiffrement AES pour le cache. Section <c>"Cache:Encryption"</c> dans <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// La clé AES doit être fournie exclusivement via Vault ou variables d'environnement sécurisées.
/// Ne jamais committer la clé en clair dans le dépôt.
/// </remarks>
public sealed class CacheEncryptionOptions
{
    /// <summary>Nom de la section dans la configuration.</summary>
    public const string SectionName = "Cache:Encryption";

    /// <summary>
    /// Clé AES-256 encodée en base64 (256 bits = 32 bytes).
    /// Exemple de génération : <c>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))</c>.
    /// En production : fournie par HashiCorp Vault ou ESO (External Secrets Operator).
    /// </summary>
    public string? Key { get; set; }
}
