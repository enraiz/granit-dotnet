namespace Granit.Notifications.MobilePush;

/// <summary>
/// Abstraction for sending mobile push notifications. Implemented by providers (FCM, APNs, etc.)
/// and registered as Keyed Services.
/// </summary>
public interface IMobilePushSender
{
    /// <summary>Sends a mobile push notification to one or more device tokens.</summary>
    Task SendAsync(MobilePushMessage message, CancellationToken ct = default);
}
