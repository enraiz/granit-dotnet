using System.Text.Json;

namespace DigitalDynamics.Foundation.Caching;

/// <summary>
/// Options de configuration du cache. Section <c>"Cache"</c> dans <c>appsettings.json</c>.
/// </summary>
public sealed class CachingOptions
{
    /// <summary>Nom de la section dans la configuration.</summary>
    public const string SectionName = "Cache";

    /// <summary>
    /// Préfixe appliqué à toutes les clés de cache.
    /// Format final : <c>{KeyPrefix}:{CacheName}:{userKey}</c>.
    /// Défaut : <c>"dd"</c>.
    /// </summary>
    public string KeyPrefix { get; set; } = "dd";

    /// <summary>
    /// Expiration absolue par défaut relative au moment de la mise en cache.
    /// Utilisée si aucune option n'est passée à <c>SetAsync</c> ou <c>GetOrAddAsync</c>.
    /// Défaut : 1 heure.
    /// </summary>
    public TimeSpan? DefaultAbsoluteExpirationRelativeToNow { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Expiration glissante par défaut (réinitialisée à chaque accès).
    /// Défaut : 20 minutes.
    /// </summary>
    public TimeSpan? DefaultSlidingExpiration { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Active le chiffrement AES-256 pour tous les types sans attribut <see cref="CacheEncryptedAttribute"/>.
    /// Les types marqués <c>[CacheEncrypted]</c> sont toujours chiffrés (prioritaire).
    /// Les types marqués <c>[CacheEncrypted(false)]</c> ne sont jamais chiffrés (prioritaire).
    /// Défaut : <c>false</c> (désactivé en dev/Memory).
    /// </summary>
    public bool EncryptValues { get; set; }

    /// <summary>
    /// Options de sérialisation JSON personnalisées. Si <c>null</c>, utilise les options par défaut.
    /// </summary>
    public JsonSerializerOptions? JsonOptions { get; set; }
}
