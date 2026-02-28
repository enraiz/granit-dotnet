using Granit.Timeline.Abstractions;
using Granit.Timeline.Domain;

namespace Granit.Timeline.Internal;

/// <summary>
/// No-op implementation of <see cref="ITimelineNotifier"/>.
/// Used when <c>Granit.Notifications</c> is not available.
/// </summary>
internal sealed class NullTimelineNotifier : ITimelineNotifier
{
    /// <inheritdoc/>
    public Task NotifyEntryPostedAsync(TimelineEntry entry, IReadOnlyList<string> followerUserIds, CancellationToken ct = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task NotifyMentionedUsersAsync(TimelineEntry entry, IReadOnlyList<string> mentionedUserIds, CancellationToken ct = default) =>
        Task.CompletedTask;
}
