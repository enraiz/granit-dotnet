using Granit.Core.Domain;

namespace Granit.Timeline.Domain;

/// <summary>
/// A single entry in the activity stream for any <see cref="ITimelined"/> entity.
/// Three entry types coexist in the same table:
/// <list type="bullet">
///   <item><see cref="TimelineEntryType.Comment"/> — human-authored, soft-deletable (RGPD).</item>
///   <item><see cref="TimelineEntryType.InternalNote"/> — human-authored, staff-only, soft-deletable.</item>
///   <item><see cref="TimelineEntryType.SystemLog"/> — auto-generated, INSERT-only immutable (HDS).</item>
/// </list>
/// </summary>
public sealed class TimelineEntry : CreationAuditedEntity, ISoftDeletable, IMultiTenant
{
    /// <summary>Entity type name (e.g. "Patient", "Invoice").</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Entity identifier as string (polymorphic reference).</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Type of entry (comment, system log, or internal note).</summary>
    public TimelineEntryType EntryType { get; set; }

    /// <summary>
    /// Entry body. Markdown for <see cref="TimelineEntryType.Comment"/> and
    /// <see cref="TimelineEntryType.InternalNote"/>, structured JSON for
    /// <see cref="TimelineEntryType.SystemLog"/>.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>User ID of the author (denormalized for display performance).</summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>Display name of the author at the time of posting (denormalized).</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Optional parent entry ID for threaded replies.</summary>
    public Guid? ParentEntryId { get; set; }

    /// <inheritdoc/>
    public Guid? TenantId { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc/>
    public string? DeletedBy { get; set; }
}
