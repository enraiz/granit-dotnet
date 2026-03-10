namespace Granit.Caching.StackExchangeRedis.Options;

/// <summary>
/// Options de configuration du fournisseur Redis. Section <c>"Cache:Redis"</c> dans <c>appsettings.json</c>.
/// </summary>
public sealed class RedisCachingOptions
{
    /// <summary>Nom de la section dans la configuration.</summary>
    public const string SectionName = "Cache:Redis";

    /// <summary>
    /// Active ou désactive le fournisseur Redis.
    /// Si <c>false</c>, le fournisseur Memory reste actif.
    /// Utile pour désactiver Redis en développement sans modifier le module chargé.
    /// Défaut : <c>true</c>.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Chaîne de connexion Redis au format StackExchange.Redis.
    /// Exemples : <c>"localhost:6379"</c>, <c>"redis-service:6379,password=secret"</c>.
    /// En production : fournie via Vault ou variables d'environnement.
    /// </summary>
    public string Configuration { get; set; } = "localhost:6379";

    /// <summary>
    /// Préfixe de l'instance Redis. Préfixe toutes les clés stockées dans Redis.
    /// Permet d'isoler plusieurs applications sur la même instance Redis.
    /// Défaut : <c>"dd:"</c>.
    /// </summary>
    public string InstanceName { get; set; } = "dd:";
}
