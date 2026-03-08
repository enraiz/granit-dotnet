using Granit.Core.MultiTenancy;
using Granit.Notifications.Abstractions;
using Granit.Timeline.Abstractions;

namespace Granit.Timeline.Notifications.Internal;

/// <summary>
/// Implements <see cref="ITimelineFollowerService"/> by delegating to
/// <see cref="INotificationSubscriptionReader"/> and <see cref="INotificationSubscriptionWriter"/>
/// entity follower methods.
/// </summary>
internal sealed class NotificationBackedFollowerService(
    INotificationSubscriptionReader subscriptionReader,
    INotificationSubscriptionWriter subscriptionWriter,
    ICurrentTenant currentTenant) : ITimelineFollowerService
{
    /// <inheritdoc/>
    public Task FollowAsync(string userId, string entityType, string entityId, CancellationToken ct = default) =>
        subscriptionWriter.FollowEntityAsync(userId, entityType, entityId, TenantId, ct);

    /// <inheritdoc/>
    public Task UnfollowAsync(string userId, string entityType, string entityId, CancellationToken ct = default) =>
        subscriptionWriter.UnfollowEntityAsync(userId, entityType, entityId, TenantId, ct);

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetFollowerIdsAsync(string entityType, string entityId, CancellationToken ct = default) =>
        subscriptionReader.GetEntityFollowerIdsAsync(entityType, entityId, TenantId, ct);

    /// <inheritdoc/>
    public Task<bool> IsFollowingAsync(string userId, string entityType, string entityId, CancellationToken ct = default) =>
        subscriptionReader.IsFollowingEntityAsync(userId, entityType, entityId, TenantId, ct);

    private Guid? TenantId => currentTenant.IsAvailable ? currentTenant.Id : null;
}
