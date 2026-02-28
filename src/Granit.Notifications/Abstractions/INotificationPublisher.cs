using System.Text.Json;

namespace Granit.Notifications.Abstractions;

/// <summary>
/// Application-facing facade for publishing notification triggers into the dispatch engine.
/// </summary>
public interface INotificationPublisher
{
    /// <summary>
    /// Publishes a notification to explicit recipients.
    /// </summary>
    ValueTask PublishAsync<TData>(
        NotificationType<TData> notificationType,
        TData data,
        IReadOnlyList<string> recipientUserIds,
        CancellationToken ct = default) where TData : notnull;

    /// <summary>
    /// Publishes a notification to explicit recipients with a related entity reference (Odoo-style).
    /// </summary>
    ValueTask PublishAsync<TData>(
        NotificationType<TData> notificationType,
        TData data,
        IReadOnlyList<string> recipientUserIds,
        EntityReference? relatedEntity,
        CancellationToken ct = default) where TData : notnull;

    /// <summary>
    /// Publishes a notification to all subscribers of the given notification type.
    /// </summary>
    ValueTask PublishToSubscribersAsync<TData>(
        NotificationType<TData> notificationType,
        TData data,
        CancellationToken ct = default) where TData : notnull;

    /// <summary>
    /// Publishes a notification to all followers of the given entity (Odoo-style).
    /// </summary>
    ValueTask PublishToEntityFollowersAsync<TData>(
        NotificationType<TData> notificationType,
        TData data,
        EntityReference relatedEntity,
        CancellationToken ct = default) where TData : notnull;
}
