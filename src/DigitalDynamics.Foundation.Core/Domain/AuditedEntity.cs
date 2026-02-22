// =============================================================================
// AuditedEntity - Entity with full HDS creation/modification traceability
// =============================================================================
// Adds modification fields (ModifiedAt, ModifiedBy) to CreationAuditedEntity.
// Replaces the older AuditableEntity.
//
// HDS compliance: every modification is tracked with the user and timestamp.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Domain;

/// <summary>
/// Entity with full audit trail (creation + modification).
/// Inherits from <see cref="CreationAuditedEntity"/> and adds ModifiedAt/ModifiedBy.
/// </summary>
public abstract class AuditedEntity : CreationAuditedEntity
{
    /// <summary>Last modification timestamp (UTC).</summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>Identifier of the user who last modified the entity.</summary>
    public string? ModifiedBy { get; set; }
}
