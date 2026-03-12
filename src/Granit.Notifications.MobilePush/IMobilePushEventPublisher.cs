namespace Granit.Notifications.MobilePush;

/// <summary>
/// Publishes mobile push domain events (e.g. token invalidation).
/// </summary>
/// <remarks>
/// The default implementation is a no-op that logs a warning. When Wolverine is installed,
/// events are published via <c>IMessageBus</c> for durable, async processing.
/// </remarks>
public interface IMobilePushEventPublisher
{
    /// <summary>
    /// Publishes a <see cref="MobilePushTokenInvalidated"/> event to notify the system
    /// that a device token should be removed from the token store.
    /// </summary>
    Task PublishTokenInvalidatedAsync(
        MobilePushTokenInvalidated tokenInvalidated,
        CancellationToken cancellationToken = default);
}
