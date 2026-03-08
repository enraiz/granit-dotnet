using Granit.Notifications.Domain;

namespace Granit.Notifications.Abstractions;

/// <summary>
/// INSERT-only audit trail for notification delivery attempts (HDS compliance).
/// </summary>
public interface INotificationDeliveryWriter
{
    Task RecordAsync(NotificationDeliveryAttempt attempt, CancellationToken ct = default);
}
