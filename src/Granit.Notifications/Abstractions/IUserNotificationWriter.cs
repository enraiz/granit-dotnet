using Granit.Notifications.Domain;

namespace Granit.Notifications.Abstractions;

/// <summary>
/// Write operations for in-app user notifications (inbox).
/// </summary>
public interface IUserNotificationWriter
{
    Task InsertAsync(UserNotification notification, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid id, DateTimeOffset readAt, CancellationToken ct = default);
    Task MarkAllAsReadAsync(string recipientUserId, Guid? tenantId, DateTimeOffset readAt, CancellationToken ct = default);
}
