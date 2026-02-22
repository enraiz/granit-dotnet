// =============================================================================
// IMultiTenant - Interface pour les entités multi-tenant
// =============================================================================
// Implémentée par les entités dont les données sont isolées par tenant.
// Le TenantId est automatiquement rempli par AuditedEntityInterceptor
// lors de la création, depuis ICurrentTenant (AsyncLocal).
//
// Inspiration : ABP Framework IMultiTenant<T>
// Conformité RGPD : TenantId est un GUID pseudonymisé — ne jamais stocker
// de données nominatives dans ce champ.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Domain;

/// <summary>
/// Interface pour les entités appartenant à un tenant spécifique.
/// Le <see cref="TenantId"/> est automatiquement défini par
/// <c>AuditedEntityInterceptor</c> lors de la persistance (SaveChanges).
/// </summary>
public interface IMultiTenant
{
    /// <summary>
    /// Identifiant du tenant propriétaire de cette entité.
    /// <c>null</c> indique une donnée globale, partagée entre tous les tenants.
    /// </summary>
    Guid? TenantId { get; set; }
}
