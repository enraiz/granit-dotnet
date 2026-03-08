namespace Granit.Timeline.Abstractions;

/// <summary>
/// Facade for entity follower management.
/// When <c>Granit.Timeline.Notifications</c> is installed, delegates to
/// <c>INotificationSubscriptionStore</c>. Otherwise falls back to a standalone
/// in-memory implementation.
/// </summary>
public interface ITimelineFollowerService
{
    /// <summary>Subscribes a user as a follower of an entity.</summary>
    Task FollowAsync(string userId, string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>Unsubscribes a user from an entity.</summary>
    Task UnfollowAsync(string userId, string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>Returns the user IDs of all followers of an entity.</summary>
    Task<IReadOnlyList<string>> GetFollowerIdsAsync(string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>Returns whether a user is following an entity.</summary>
    Task<bool> IsFollowingAsync(string userId, string entityType, string entityId, CancellationToken cancellationToken = default);
}
