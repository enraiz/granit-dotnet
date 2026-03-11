using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

using Granit.Core.Domain;

namespace Granit.ReferenceData.Domain;

/// <summary>
/// Abstract base class for all reference data entities (countries, currencies, languages, etc.).
/// Inherits ISO 27001 audit trail from <see cref="AuditedEntity"/> and participates in the
/// <see cref="IActive"/> global query filter registered by <c>ApplyGranitConventions()</c>.
/// </summary>
/// <remarks>
/// <para>
/// Each reference data entity has a unique <see cref="Code"/> (business key) and labels
/// for the 7 supported locales (en, fr, nl, de, es, it, pt). The virtual <see cref="Label"/>
/// property resolves the appropriate label based on <see cref="CultureInfo.CurrentUICulture"/>,
/// falling back to <see cref="LabelEn"/> when the requested locale has no value.
/// Derived entities can override <see cref="Label"/> for custom resolution logic.
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
    /// English display label (fallback). Always persisted; used when no translation
    /// is available for the requested culture.
    /// </summary>
    public string LabelEn { get; set; } = string.Empty;

    /// <summary>French display label.</summary>
    public string LabelFr { get; set; } = string.Empty;

    /// <summary>Dutch display label.</summary>
    public string LabelNl { get; set; } = string.Empty;

    /// <summary>German display label.</summary>
    public string LabelDe { get; set; } = string.Empty;

    /// <summary>Spanish display label.</summary>
    public string LabelEs { get; set; } = string.Empty;

    /// <summary>Italian display label.</summary>
    public string LabelIt { get; set; } = string.Empty;

    /// <summary>Portuguese display label.</summary>
    public string LabelPt { get; set; } = string.Empty;

    /// <summary>
    /// Resolved display label for the current UI culture. Returns the locale-specific
    /// label when available, falling back to <see cref="LabelEn"/> when the translation
    /// is empty or the culture is not in the supported set (fr, nl, de, es, it, pt).
    /// </summary>
    /// <remarks>
    /// This property is not mapped to the database. It is intended for use in application
    /// code and API responses where culture-specific labels are needed.
    /// Override in derived entities to provide custom resolution logic.
    /// </remarks>
    [NotMapped]
    public virtual string Label => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "fr" when LabelFr.Length > 0 => LabelFr,
        "nl" when LabelNl.Length > 0 => LabelNl,
        "de" when LabelDe.Length > 0 => LabelDe,
        "es" when LabelEs.Length > 0 => LabelEs,
        "it" when LabelIt.Length > 0 => LabelIt,
        "pt" when LabelPt.Length > 0 => LabelPt,
        _ => LabelEn,
    };

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
