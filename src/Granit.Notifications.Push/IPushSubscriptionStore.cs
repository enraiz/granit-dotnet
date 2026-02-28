namespace Granit.Notifications.Push;

/// <summary>Store for browser push subscriptions per user.</summary>
public interface IPushSubscriptionStore
{
    /// <summary>Gets all push subscriptions for a user.</summary>
    Task<IReadOnlyList<PushSubscriptionInfo>> GetSubscriptionsAsync(
        string userId, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Saves a push subscription for a user.</summary>
    Task SaveSubscriptionAsync(
        string userId, PushSubscriptionInfo subscription, Guid? tenantId, CancellationToken ct = default);

    /// <summary>Removes a push subscription by endpoint.</summary>
    Task RemoveSubscriptionAsync(
        string endpoint, Guid? tenantId, CancellationToken ct = default);
}
