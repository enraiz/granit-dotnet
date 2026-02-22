// =============================================================================
// CreationAuditedEntity - Entité avec traçabilité de création HDS
// =============================================================================
// Ajoute les champs de création (CreatedAt, CreatedBy) à Entity.
// Adaptée aux entités immuables (entrées d'audit, transactions, consentements).
//
// Les champs sont remplis automatiquement par AuditedEntityInterceptor
// dans le package Persistence.
//
// Conformité HDS : traçabilité de la création avec utilisateur et horodatage.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Domain;

/// <summary>
/// Entité avec audit trail de création uniquement.
/// Hérite de <see cref="Entity"/> et ajoute CreatedAt/CreatedBy.
/// </summary>
public abstract class CreationAuditedEntity : Entity
{
    /// <summary>Date de création (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Identifiant de l'utilisateur ayant créé l'entité.</summary>
    public string CreatedBy { get; set; } = string.Empty;
}
