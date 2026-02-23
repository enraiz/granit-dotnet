namespace Granit.Core.Domain;

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
