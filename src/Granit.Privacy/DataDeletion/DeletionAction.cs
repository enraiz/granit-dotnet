namespace Granit.Privacy.DataDeletion;

/// <summary>
/// Action taken by a data provider when handling a personal data deletion request.
/// Used for audit trail (HDS/ISO 27001 compliance).
/// </summary>
public enum DeletionAction
{
    /// <summary>Data was permanently removed from the database.</summary>
    PhysicalDelete = 0,

    /// <summary>Data was soft-deleted via <c>ISoftDeletable</c>.</summary>
    SoftDelete = 1,

    /// <summary>PII was replaced with pseudonymized values (HDS retention — data preserved, identity removed).</summary>
    Anonymized = 2,

    /// <summary>Data was retained as-is due to a legal obligation (e.g., HDS 20-year retention).</summary>
    Retained = 3,

    /// <summary>Combination of multiple actions (e.g., PII anonymized + medical data retained).</summary>
    Mixed = 4
}
