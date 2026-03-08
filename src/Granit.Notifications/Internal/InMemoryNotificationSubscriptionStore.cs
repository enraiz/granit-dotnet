using System.Collections.Concurrent;
using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;

namespace Granit.Notifications.Internal;

internal sealed class InMemoryNotificationSubscriptionStore : INotificationSubscriptionReader, INotificationSubscriptionWriter
{
    private readonly ConcurrentDictionary<string, NotificationSubscription> _subscriptions = new();

    private static string BuildKey(string userId, string typeName, Guid? tenantId, string? entityType = null, string? entityId = null) =>
        $"{tenantId}:{userId}:{typeName}:{entityType}:{entityId}";

    public Task SubscribeAsync(string userId, string notificationTypeName, Guid? tenantId, CancellationToken ct = default)
    {
        string key = BuildKey(userId, notificationTypeName, tenantId);
        _subscriptions.TryAdd(key, new NotificationSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            NotificationTypeName = notificationTypeName,
            TenantId = tenantId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string userId, string notificationTypeName, Guid? tenantId, CancellationToken ct = default)
    {
        string key = BuildKey(userId, notificationTypeName, tenantId);
        _subscriptions.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetSubscriberIdsAsync(string notificationTypeName, Guid? tenantId, CancellationToken ct = default)
    {
        IReadOnlyList<string> result = _subscriptions.Values
            .Where(s => s.NotificationTypeName == notificationTypeName && s.TenantId == tenantId && s.EntityType is null)
            .Select(s => s.UserId)
            .Distinct()
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<NotificationSubscription>> GetUserSubscriptionsAsync(string userId, Guid? tenantId, CancellationToken ct = default)
    {
        IReadOnlyList<NotificationSubscription> result = _subscriptions.Values
            .Where(s => s.UserId == userId && s.TenantId == tenantId)
            .ToList();
        return Task.FromResult(result);
    }

    public Task FollowEntityAsync(string userId, string entityType, string entityId, Guid? tenantId, CancellationToken ct = default)
    {
        string key = BuildKey(userId, string.Empty, tenantId, entityType, entityId);
        _subscriptions.TryAdd(key, new NotificationSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            NotificationTypeName = string.Empty,
            EntityType = entityType,
            EntityId = entityId,
            TenantId = tenantId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        return Task.CompletedTask;
    }

    public Task UnfollowEntityAsync(string userId, string entityType, string entityId, Guid? tenantId, CancellationToken ct = default)
    {
        string key = BuildKey(userId, string.Empty, tenantId, entityType, entityId);
        _subscriptions.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetEntityFollowerIdsAsync(string entityType, string entityId, Guid? tenantId, CancellationToken ct = default)
    {
        IReadOnlyList<string> result = _subscriptions.Values
            .Where(s => s.EntityType == entityType && s.EntityId == entityId && s.TenantId == tenantId)
            .Select(s => s.UserId)
            .Distinct()
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<NotificationSubscription>> GetEntityFollowersAsync(string entityType, string entityId, Guid? tenantId, CancellationToken ct = default)
    {
        IReadOnlyList<NotificationSubscription> result = _subscriptions.Values
            .Where(s => s.EntityType == entityType && s.EntityId == entityId && s.TenantId == tenantId)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<bool> IsFollowingEntityAsync(string userId, string entityType, string entityId, Guid? tenantId, CancellationToken ct = default)
    {
        string key = BuildKey(userId, string.Empty, tenantId, entityType, entityId);
        return Task.FromResult(_subscriptions.ContainsKey(key));
    }
}
