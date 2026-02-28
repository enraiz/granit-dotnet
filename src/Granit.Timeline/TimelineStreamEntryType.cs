namespace Granit.Timeline;

/// <summary>
/// Discriminator for <see cref="TimelineStreamEntry"/> in the unified activity stream.
/// Extends <see cref="Domain.TimelineEntryType"/> with future sources.
/// </summary>
public enum TimelineStreamEntryType
{
    /// <summary>Human-authored comment.</summary>
    Comment = 0,

    /// <summary>Human-authored internal note (staff-only).</summary>
    InternalNote = 1,

    /// <summary>Auto-generated system log entry.</summary>
    SystemLog = 2,
}
