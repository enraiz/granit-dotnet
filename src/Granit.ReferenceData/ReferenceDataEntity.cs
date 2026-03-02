using System.ComponentModel.DataAnnotations.Schema;

using Granit.Core.Domain;

namespace Granit.ReferenceData;

/// <summary>
/// Abstract base class for all reference data entities (countries, currencies, languages, etc.).
/// Inherits HDS audit trail from <see cref="AuditedEntity"/> and participates in the
/// <see cref="IActive"/> global query filter registered by <c>ApplyGranitConventions()</c>.
/// </summary>
/// <remarks>
/// <para>
/// Each reference data entity has a unique <see cref="Code"/> (business key) and an English
/// label (<see cref="LabelEn"/>). The virtual <see cref="Label"/> property returns <see cref="LabelEn"/>
/// by default; derived entities can override it to resolve the label based on the current culture
/// (e.g., returning LabelFr when <c>CultureInfo.CurrentUICulture</c> is "fr").
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
    /// English display label. Persisted in the database. Applications needing translations
    /// should add locale-specific properties (e.g., LabelFr, LabelNl) and override
    /// <see cref="Label"/> to resolve based on the current culture.
    /// </summary>
    public string LabelEn { get; set; } = string.Empty;

    /// <summary>
    /// Resolved display label for the current culture. Returns <see cref="LabelEn"/> by default.
    /// Override in derived entities to provide culture-aware label resolution.
    /// </summary>
    /// <remarks>
    /// This property is not mapped to the database. It is intended for use in application
    /// code and API responses where culture-specific labels are needed.
    /// </remarks>
    [NotMapped]
    public virtual string Label => LabelEn;

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
