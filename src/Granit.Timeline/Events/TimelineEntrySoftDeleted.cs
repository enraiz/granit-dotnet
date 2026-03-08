using Granit.Core.Events;

namespace Granit.Timeline.Events;

/// <summary>
/// Raised when a timeline entry is soft-deleted (RGPD right to erasure).
/// System log entries cannot be deleted (HDS audit trail).
/// </summary>
public sealed record TimelineEntrySoftDeleted(
    Guid EntryId,
    string EntityType,
    string EntityId) : IDomainEvent;
