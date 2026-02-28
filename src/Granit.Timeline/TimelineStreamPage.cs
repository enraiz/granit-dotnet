namespace Granit.Timeline;

/// <summary>
/// Paginated result for the unified activity stream.
/// </summary>
public sealed record TimelineStreamPage
{
    /// <summary>Entries for the current page, ordered by <c>OccurredAt</c> descending.</summary>
    public required IReadOnlyList<TimelineStreamEntry> Items { get; init; }

    /// <summary>Total number of entries across all pages.</summary>
    public required int TotalCount { get; init; }
}
