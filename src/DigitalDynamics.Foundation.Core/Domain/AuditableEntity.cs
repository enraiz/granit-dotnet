// =============================================================================
// AuditableEntity - Base class for HDS audit (3-year retention)
// =============================================================================
// Every persisted entity MUST inherit from AuditableEntity to guarantee
// the traceability required by HDS certification.
//
// Fields are populated automatically by AuditableEntityInterceptor
// in the Persistence package.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Domain;

/// <summary>
/// Base class for all entities with an HDS audit trail.
/// Provides creation/modification traceability fields.
/// </summary>
public abstract class AuditableEntity
{
    /// <summary>Unique identifier of the entity.</summary>
    public Guid Id { get; set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Identifier of the user who created the entity.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Last modification timestamp (UTC).</summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>Identifier of the user who last modified the entity.</summary>
    public string? ModifiedBy { get; set; }
}
