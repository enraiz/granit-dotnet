using Granit.Notifications.Domain;

namespace Granit.Notifications.Abstractions;

/// <summary>
/// Store for notification subscriptions (topic subscriptions + entity followers).
/// </summary>
public interface INotificationSubscriptionStore
{
    Task SubscribeAsync(string userId, string notificationTypeName, Guid? tenantId, CancellationToken ct = default);
    Task UnsubscribeAsync(string userId, string notificationTypeName, Guid? tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetSubscriberIdsAsync(string notificationTypeName, Guid? tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationSubscription>> GetUserSubscriptionsAsync(string userId, Guid? tenantId, CancellationToken ct = default);

    // Entity followers (Odoo-style)
    Task FollowEntityAsync(string userId, string entityType, string entityId, Guid? tenantId, CancellationToken ct = default);
    Task UnfollowEntityAsync(string userId, string entityType, string entityId, Guid? tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetEntityFollowerIdsAsync(string entityType, string entityId, Guid? tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationSubscription>> GetEntityFollowersAsync(string entityType, string entityId, Guid? tenantId, CancellationToken ct = default);
    Task<bool> IsFollowingEntityAsync(string userId, string entityType, string entityId, Guid? tenantId, CancellationToken ct = default);
}
