using Granit.Notifications.Domain;

namespace Granit.Notifications.Abstractions;

/// <summary>
/// CRUD store for in-app user notifications (inbox).
/// </summary>
public interface IUserNotificationStore
{
    Task InsertAsync(UserNotification notification, CancellationToken ct = default);
    Task<UserNotification?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<UserNotification>> GetListAsync(string recipientUserId, Guid? tenantId, int skipCount, int maxResultCount, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(string recipientUserId, Guid? tenantId, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid id, DateTimeOffset readAt, CancellationToken ct = default);
    Task MarkAllAsReadAsync(string recipientUserId, Guid? tenantId, DateTimeOffset readAt, CancellationToken ct = default);
    Task<IReadOnlyList<UserNotification>> GetByEntityAsync(string entityType, string entityId, Guid? tenantId, int skipCount, int maxResultCount, CancellationToken ct = default);
}
