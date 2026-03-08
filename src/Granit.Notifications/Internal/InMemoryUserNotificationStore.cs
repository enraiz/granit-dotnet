using System.Collections.Concurrent;
using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Granit.Querying;

namespace Granit.Notifications.Internal;

internal sealed class InMemoryUserNotificationStore : IUserNotificationReader, IUserNotificationWriter
{
    private readonly ConcurrentDictionary<Guid, UserNotification> _notifications = new();

    public Task InsertAsync(UserNotification notification, CancellationToken ct = default)
    {
        _notifications[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Task<UserNotification?> GetAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_notifications.GetValueOrDefault(id));

    public Task<PagedResult<UserNotification>> GetListAsync(string recipientUserId, Guid? tenantId, int page = 1, int pageSize = QueryingDefaults.DefaultPageSize, CancellationToken ct = default)
    {
        int clampedPage = Math.Max(page, 1);
        int clampedPageSize = Math.Clamp(pageSize, 1, QueryingDefaults.MaxPageSize);

        var filtered = _notifications.Values
            .Where(n => n.RecipientUserId == recipientUserId && n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        int totalCount = filtered.Count;
        IReadOnlyList<UserNotification> items = filtered
            .Skip((clampedPage - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .ToList();

        return Task.FromResult(new PagedResult<UserNotification>(items, totalCount));
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

    public Task<PagedResult<UserNotification>> GetByEntityAsync(string entityType, string entityId, Guid? tenantId, int page = 1, int pageSize = QueryingDefaults.DefaultPageSize, CancellationToken ct = default)
    {
        int clampedPage = Math.Max(page, 1);
        int clampedPageSize = Math.Clamp(pageSize, 1, QueryingDefaults.MaxPageSize);

        var filtered = _notifications.Values
            .Where(n => n.RelatedEntityType == entityType && n.RelatedEntityId == entityId && n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        int totalCount = filtered.Count;
        IReadOnlyList<UserNotification> items = filtered
            .Skip((clampedPage - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .ToList();

        return Task.FromResult(new PagedResult<UserNotification>(items, totalCount));
    }
}
