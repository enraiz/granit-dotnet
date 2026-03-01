namespace Granit.Timeline.Abstractions;

/// <summary>
/// Query service for the unified activity stream (read operations).
/// Returns paginated, chronologically ordered entries for a given entity.
/// </summary>
public interface ITimelineQuery
{
    /// <summary>
    /// Returns a paginated activity stream for a specific entity.
    /// Entries are ordered by <c>OccurredAt</c> descending (newest first).
    /// </summary>
    Task<TimelineStreamPage> GetStreamAsync(
        string entityType,
        string entityId,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default);
}
