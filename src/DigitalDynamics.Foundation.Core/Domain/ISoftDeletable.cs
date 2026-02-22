// =============================================================================
// ISoftDeletable - Soft delete interface (GDPR)
// =============================================================================
// Entities containing personal data implement this interface
// to support the GDPR right to erasure via soft deletion.
//
// The SoftDeleteInterceptor (Persistence package) intercepts DELETE
// and converts them to UPDATE SET IsDeleted = true.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Domain;

/// <summary>
/// Interface for GDPR soft deletion.
/// Marked entities are no longer returned by standard queries
/// but remain in the database for the HDS audit trail.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>Indicates whether the entity has been soft-deleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Soft deletion timestamp (UTC).</summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Identifier of the user who deleted the entity.</summary>
    string? DeletedBy { get; set; }
}
