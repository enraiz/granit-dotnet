namespace Granit.Notifications.MobilePush;

/// <summary>
/// Event published when a device token is no longer valid (e.g. FCM returns UNREGISTERED).
/// Handled to clean up the token store.
/// </summary>
public sealed record MobilePushTokenInvalidated
{
    /// <summary>The invalid device token.</summary>
    public required string DeviceToken { get; init; }

    /// <summary>Tenant identifier.</summary>
    public Guid? TenantId { get; init; }
}
