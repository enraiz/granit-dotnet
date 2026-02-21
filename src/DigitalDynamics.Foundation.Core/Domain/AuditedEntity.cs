// =============================================================================
// AuditedEntity - Entité avec traçabilité complète création/modification HDS
// =============================================================================
// Ajoute les champs de modification (ModifiedAt, ModifiedBy) à
// CreationAuditedEntity. Remplace l'ancien AuditableEntity.
//
// Conformité HDS : chaque modification est tracée avec l'utilisateur
// et l'horodatage.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Domain;

/// <summary>
/// Entité avec audit trail complet (création + modification).
/// Hérite de <see cref="CreationAuditedEntity"/> et ajoute ModifiedAt/ModifiedBy.
/// </summary>
public abstract class AuditedEntity : CreationAuditedEntity
{
    /// <summary>Date de dernière modification (UTC).</summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>Identifiant de l'utilisateur ayant modifié l'entité.</summary>
    public string? ModifiedBy { get; set; }
}
