namespace Granit.Notifications.Push;

/// <summary>Read operations for browser push subscriptions per user.</summary>
public interface IPushSubscriptionReader
{
    /// <summary>Gets all push subscriptions for a user.</summary>
    Task<IReadOnlyList<PushSubscriptionInfo>> GetSubscriptionsAsync(
        string userId, Guid? tenantId, CancellationToken cancellationToken = default);
}
