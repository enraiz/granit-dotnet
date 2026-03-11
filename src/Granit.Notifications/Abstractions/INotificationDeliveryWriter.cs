using Granit.Notifications.Domain;

namespace Granit.Notifications.Abstractions;

/// <summary>
/// INSERT-only audit trail for notification delivery attempts (ISO 27001 compliance).
/// </summary>
public interface INotificationDeliveryWriter
{
    Task RecordAsync(NotificationDeliveryAttempt attempt, CancellationToken cancellationToken = default);
}
