namespace DigitalDynamics.Foundation.Caching.Hybrid;

/// <summary>
/// Options de configuration du fournisseur HybridCache (L1+L2).
/// Section <c>"Cache:Hybrid"</c> dans <c>appsettings.json</c>.
/// </summary>
public sealed class HybridCachingOptions
{
    /// <summary>Nom de la section dans la configuration.</summary>
    public const string SectionName = "Cache:Hybrid";

    /// <summary>
    /// Durée d'expiration du cache L1 (mémoire locale par pod).
    /// Doit être court pour limiter la fenêtre de données obsolètes entre pods Kubernetes.
    /// Valeur maximale recommandée : 60 secondes.
    /// Défaut : 30 secondes.
    /// </summary>
    public TimeSpan LocalCacheExpiration { get; set; } = TimeSpan.FromSeconds(30);
}
