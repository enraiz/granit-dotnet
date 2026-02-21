// =============================================================================
// MultiTenancyOptions - Configuration du module MultiTenancy
// =============================================================================

namespace DigitalDynamics.Foundation.MultiTenancy;

/// <summary>
/// Options de configuration du module MultiTenancy.
/// </summary>
public sealed class MultiTenancyOptions
{
    /// <summary>Nom de la section de configuration.</summary>
    public const string SectionName = "MultiTenancy";

    /// <summary>
    /// Active ou désactive la résolution du tenant par le middleware.
    /// Désactiver en environnement mono-tenant ou pour les tests.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Type du claim JWT contenant l'identifiant du tenant.
    /// Valeur par défaut : "tenant_id" (claim Keycloak standard).
    /// </summary>
    public string TenantIdClaimType { get; set; } = "tenant_id";

    /// <summary>
    /// Nom du header HTTP contenant l'identifiant du tenant.
    /// Valeur par défaut : "X-Tenant-Id".
    /// </summary>
    public string TenantIdHeaderName { get; set; } = "X-Tenant-Id";
}
