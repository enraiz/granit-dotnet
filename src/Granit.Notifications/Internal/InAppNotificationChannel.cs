using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Granit.Timing;

namespace Granit.Notifications.Internal;

/// <summary>
/// Built-in InApp channel: persists notifications in the user's inbox
/// via <see cref="IUserNotificationWriter"/>. This is the "source of truth" (Django lesson).
/// </summary>
internal sealed class InAppNotificationChannel(
    IUserNotificationWriter userNotificationWriter,
    IClock clock) : INotificationChannel
{
    public string Name => NotificationChannels.InApp;

    public async Task SendAsync(NotificationDeliveryContext context, CancellationToken cancellationToken = default)
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

        await userNotificationWriter.InsertAsync(notification, cancellationToken).ConfigureAwait(false);
    }
}
