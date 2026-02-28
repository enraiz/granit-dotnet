using System.Collections.Concurrent;
using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;

namespace Granit.Notifications.Internal;

internal sealed class InMemoryUserNotificationStore : IUserNotificationStore
{
    private readonly ConcurrentDictionary<Guid, UserNotification> _notifications = new();

    public Task InsertAsync(UserNotification notification, CancellationToken ct = default)
    {
        _notifications[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Task<UserNotification?> GetAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_notifications.GetValueOrDefault(id));

    public Task<IReadOnlyList<UserNotification>> GetListAsync(string recipientUserId, Guid? tenantId, int skipCount, int maxResultCount, CancellationToken ct = default)
    {
        IReadOnlyList<UserNotification> result = _notifications.Values
            .Where(n => n.RecipientUserId == recipientUserId && n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<int> GetUnreadCountAsync(string recipientUserId, Guid? tenantId, CancellationToken ct = default)
    {
        int count = _notifications.Values
            .Count(n => n.RecipientUserId == recipientUserId && n.TenantId == tenantId && n.State == UserNotificationState.Unread);
        return Task.FromResult(count);
    }

    public Task MarkAsReadAsync(Guid id, DateTimeOffset readAt, CancellationToken ct = default)
    {
        if (_notifications.TryGetValue(id, out UserNotification? notification))
        {
            notification.State = UserNotificationState.Read;
            notification.ReadAt = readAt;
        }
        return Task.CompletedTask;
    }

    public Task MarkAllAsReadAsync(string recipientUserId, Guid? tenantId, DateTimeOffset readAt, CancellationToken ct = default)
    {
        foreach (UserNotification notification in _notifications.Values
            .Where(n => n.RecipientUserId == recipientUserId && n.TenantId == tenantId && n.State == UserNotificationState.Unread))
        {
            notification.State = UserNotificationState.Read;
            notification.ReadAt = readAt;
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<UserNotification>> GetByEntityAsync(string entityType, string entityId, Guid? tenantId, int skipCount, int maxResultCount, CancellationToken ct = default)
    {
        IReadOnlyList<UserNotification> result = _notifications.Values
            .Where(n => n.RelatedEntityType == entityType && n.RelatedEntityId == entityId && n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToList();
        return Task.FromResult(result);
    }
}
