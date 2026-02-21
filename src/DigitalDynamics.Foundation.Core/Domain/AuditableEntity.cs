// =============================================================================
// AuditableEntity - Classe de base pour l'audit HDS (3 ans de retention)
// =============================================================================
// Toute entite persistee DOIT heriter de AuditableEntity pour garantir
// la tracabilite requise par la certification HDS.
//
// Les champs sont remplis automatiquement par AuditableEntityInterceptor
// dans le package Persistence.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Domain;

/// <summary>
/// Classe de base pour toutes les entites avec audit trail HDS.
/// Fournit les champs de tracabilite creation/modification.
/// </summary>
public abstract class AuditableEntity
{
    /// <summary>Identifiant unique de l'entite.</summary>
    public Guid Id { get; set; }

    /// <summary>Date de creation (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Identifiant de l'utilisateur ayant cree l'entite.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Date de derniere modification (UTC).</summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>Identifiant de l'utilisateur ayant modifie l'entite.</summary>
    public string? ModifiedBy { get; set; }
}
