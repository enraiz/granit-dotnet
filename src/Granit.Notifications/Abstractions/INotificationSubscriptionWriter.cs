namespace Granit.Notifications.Abstractions;

/// <summary>
/// Write operations for notification subscriptions (topic subscriptions + entity followers).
/// </summary>
public interface INotificationSubscriptionWriter
{
    Task SubscribeAsync(string userId, string notificationTypeName, Guid? tenantId, CancellationToken ct = default);
    Task UnsubscribeAsync(string userId, string notificationTypeName, Guid? tenantId, CancellationToken ct = default);

    // Entity followers (Odoo-style)
    Task FollowEntityAsync(string userId, string entityType, string entityId, Guid? tenantId, CancellationToken ct = default);
    Task UnfollowEntityAsync(string userId, string entityType, string entityId, Guid? tenantId, CancellationToken ct = default);
}
