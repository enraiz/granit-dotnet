using Granit.Notifications.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace Granit.Notifications.SignalR.Internal;

/// <summary>
/// Notification channel that pushes notifications to connected SignalR clients
/// via the user's group (all tabs/devices).
/// </summary>
internal sealed class SignalRNotificationChannel(IHubContext<NotificationHub> hubContext) : INotificationChannel
{
    /// <inheritdoc/>
    public string Name => NotificationChannels.SignalR;

    /// <inheritdoc/>
    public async Task SendAsync(NotificationDeliveryContext context, CancellationToken cancellationToken = default)
    {
        SignalRNotificationMessage message = new()
        {
            NotificationId = context.NotificationId,
            NotificationTypeName = context.NotificationTypeName,
            Severity = context.Severity,
            Data = context.Data,
            RelatedEntityType = context.RelatedEntity?.EntityType,
            RelatedEntityId = context.RelatedEntity?.EntityId,
            OccurredAt = context.OccurredAt,
        };

        await hubContext.Clients
            .Group(context.RecipientUserId)
            .SendAsync("ReceiveNotification", message, cancellationToken).ConfigureAwait(false);
    }
}
