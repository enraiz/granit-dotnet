using Granit.Core.Domain;

namespace Granit.ReferenceData;

/// <summary>
/// Abstract base class for all reference data entities (countries, currencies, languages, etc.).
/// Inherits HDS audit trail from <see cref="AuditedEntity"/> and participates in the
/// <see cref="IActive"/> global query filter registered by <c>ApplyGranitConventions()</c>.
/// </summary>
/// <remarks>
/// <para>
/// Each reference data entity has a unique <see cref="Code"/> (business key) and a default
/// <see cref="Label"/> (English). Applications extend this class to add type-specific properties
/// (e.g., Alpha3Code, CallingCode for a Country entity).
/// </para>
/// <para>
/// Soft activation/deactivation is controlled by <see cref="IsActive"/>. Deactivated entries
/// are filtered out by the EF Core global query filter unless explicitly disabled.
/// </para>
/// </remarks>
public abstract class ReferenceDataEntity : AuditedEntity, IActive
{
    /// <summary>
    /// Unique business key for the reference data entry (e.g., "BE", "EUR", "fr").
    /// Immutable after creation. Used as the lookup key in APIs and seeders.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Default display label (English). Applications needing translations should
    /// add locale-specific properties (e.g., LabelFr, LabelNl).
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <inheritdoc/>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for UI sorting. Lower values appear first. Default is 0.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Optional start date for the validity period. <c>null</c> means valid since the beginning of time.
    /// </summary>
    public DateTimeOffset? ValidFrom { get; set; }

    /// <summary>
    /// Optional end date for the validity period. <c>null</c> means valid indefinitely.
    /// </summary>
    public DateTimeOffset? ValidTo { get; set; }
}
