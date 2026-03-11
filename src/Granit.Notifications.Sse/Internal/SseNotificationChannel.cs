using Granit.Notifications.Abstractions;

namespace Granit.Notifications.Sse.Internal;

/// <summary>
/// Notification channel that pushes notifications to connected SSE clients
/// via the user's active HTTP connections (all tabs/devices).
/// </summary>
internal sealed class SseNotificationChannel(ISseConnectionManager connectionManager) : INotificationChannel
{
    /// <inheritdoc/>
    public string Name => NotificationChannels.Sse;

    /// <inheritdoc/>
    public async Task SendAsync(NotificationDeliveryContext context, CancellationToken cancellationToken = default)
    {
        SseNotificationMessage message = new()
        {
            NotificationId = context.NotificationId,
            NotificationTypeName = context.NotificationTypeName,
            Severity = context.Severity,
            Data = context.Data,
            RelatedEntityType = context.RelatedEntity?.EntityType,
            RelatedEntityId = context.RelatedEntity?.EntityId,
            OccurredAt = context.OccurredAt,
        };

        await connectionManager
            .SendToUserAsync(context.RecipientUserId, message, cancellationToken)
            .ConfigureAwait(false);
    }
}
