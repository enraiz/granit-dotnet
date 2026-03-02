using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Granit.Timing;

namespace Granit.Notifications.Internal;

/// <summary>
/// Built-in InApp channel: persists notifications in the user's inbox
/// via <see cref="IUserNotificationStore"/>. This is the "source of truth" (Django lesson).
/// </summary>
internal sealed class InAppNotificationChannel(
    IUserNotificationStore userNotificationStore,
    IClock clock) : INotificationChannel
{
    public string Name => NotificationChannels.InApp;

    public async Task SendAsync(NotificationDeliveryContext context, CancellationToken ct = default)
    {
        UserNotification notification = new()
        {
            Id = Guid.NewGuid(),
            NotificationId = context.NotificationId,
            NotificationTypeName = context.NotificationTypeName,
            Severity = context.Severity,
            RecipientUserId = context.RecipientUserId,
            Data = context.Data,
            State = UserNotificationState.Unread,
            CreatedAt = clock.Now,
            TenantId = context.TenantId,
            RelatedEntityType = context.RelatedEntity?.EntityType,
            RelatedEntityId = context.RelatedEntity?.EntityId,
        };

        await userNotificationStore.InsertAsync(notification, ct).ConfigureAwait(false);
    }
}
