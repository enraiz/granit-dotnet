using Granit.Notifications;
using Granit.Notifications.Abstractions;
using Granit.Timeline.Abstractions;
using Granit.Timeline.Domain;

namespace Granit.Timeline.Notifications.Internal;

/// <summary>
/// Implements <see cref="ITimelineNotifier"/> by publishing notifications
/// via <see cref="INotificationPublisher"/>.
/// </summary>
internal sealed class NotificationBackedNotifier(
    INotificationPublisher publisher) : ITimelineNotifier
{
    /// <inheritdoc/>
    public async Task NotifyEntryPostedAsync(
        TimelineEntry entry,
        IReadOnlyList<string> followerUserIds,
        CancellationToken ct = default)
    {
        if (followerUserIds.Count == 0)
        {
            return;
        }

        // Exclude the author from receiving their own notification
        List<string> recipients = followerUserIds
            .Where(id => id != entry.AuthorId)
            .ToList();

        if (recipients.Count == 0)
        {
            return;
        }

        TimelineCommentNotificationData data = new(
            entry.EntityType,
            entry.EntityId,
            entry.Id,
            entry.AuthorId,
            entry.AuthorName,
            entry.Body);

        EntityReference relatedEntity = new(entry.EntityType, entry.EntityId);

        await publisher.PublishAsync(
            TimelineCommentNotificationType.Instance,
            data,
            recipients,
            relatedEntity,
            ct);
    }

    /// <inheritdoc/>
    public async Task NotifyMentionedUsersAsync(
        TimelineEntry entry,
        IReadOnlyList<string> mentionedUserIds,
        CancellationToken ct = default)
    {
        if (mentionedUserIds.Count == 0)
        {
            return;
        }

        // Exclude the author from receiving their own mention notification
        List<string> recipients = mentionedUserIds
            .Where(id => id != entry.AuthorId)
            .ToList();

        if (recipients.Count == 0)
        {
            return;
        }

        TimelineMentionNotificationData data = new(
            entry.EntityType,
            entry.EntityId,
            entry.Id,
            entry.AuthorId,
            entry.AuthorName,
            entry.Body);

        EntityReference relatedEntity = new(entry.EntityType, entry.EntityId);

        await publisher.PublishAsync(
            TimelineMentionNotificationType.Instance,
            data,
            recipients,
            relatedEntity,
            ct);
    }
}
