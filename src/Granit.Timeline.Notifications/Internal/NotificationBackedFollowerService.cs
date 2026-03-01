using Granit.Core.MultiTenancy;
using Granit.Notifications.Abstractions;
using Granit.Timeline.Abstractions;

namespace Granit.Timeline.Notifications.Internal;

/// <summary>
/// Implements <see cref="ITimelineFollowerService"/> by delegating to
/// <see cref="INotificationSubscriptionStore"/> entity follower methods.
/// </summary>
internal sealed class NotificationBackedFollowerService(
    INotificationSubscriptionStore subscriptionStore,
    ICurrentTenant currentTenant) : ITimelineFollowerService
{
    /// <inheritdoc/>
    public Task FollowAsync(string userId, string entityType, string entityId, CancellationToken ct = default) =>
        subscriptionStore.FollowEntityAsync(userId, entityType, entityId, TenantId, ct);

    /// <inheritdoc/>
    public Task UnfollowAsync(string userId, string entityType, string entityId, CancellationToken ct = default) =>
        subscriptionStore.UnfollowEntityAsync(userId, entityType, entityId, TenantId, ct);

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetFollowerIdsAsync(string entityType, string entityId, CancellationToken ct = default) =>
        subscriptionStore.GetEntityFollowerIdsAsync(entityType, entityId, TenantId, ct);

    /// <inheritdoc/>
    public Task<bool> IsFollowingAsync(string userId, string entityType, string entityId, CancellationToken ct = default) =>
        subscriptionStore.IsFollowingEntityAsync(userId, entityType, entityId, TenantId, ct);

    private Guid? TenantId => currentTenant.IsAvailable ? currentTenant.Id : null;
}
