// =============================================================================
// Entity - Classe de base minimale pour toutes les entités persistées
// =============================================================================
// Fournit uniquement l'identifiant. Les entités nécessitant un audit trail
// héritent de CreationAuditedEntity ou de ses sous-classes.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Domain;

/// <summary>
/// Classe de base abstraite pour toutes les entités persistées.
/// Fournit uniquement l'identifiant unique (Guid).
/// </summary>
public abstract class Entity
{
    /// <summary>Identifiant unique de l'entité.</summary>
    public Guid Id { get; set; }
}
