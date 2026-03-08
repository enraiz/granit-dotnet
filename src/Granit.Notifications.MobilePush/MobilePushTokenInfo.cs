namespace Granit.Notifications.MobilePush;

/// <summary>Represents a registered mobile device token for push notifications.</summary>
public sealed record MobilePushTokenInfo
{
    /// <summary>User identifier.</summary>
    public required string UserId { get; init; }

    /// <summary>Device token (FCM registration token or APNs device token).</summary>
    public required string DeviceToken { get; init; }

    /// <summary>Device platform.</summary>
    public required MobilePlatform Platform { get; init; }

    /// <summary>Tenant identifier (for multi-tenant scenarios).</summary>
    public Guid? TenantId { get; init; }

    /// <summary>Registration timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
