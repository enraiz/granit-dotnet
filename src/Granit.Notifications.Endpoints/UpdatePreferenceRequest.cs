namespace Granit.Notifications.Endpoints;

/// <summary>
/// Request body for updating a notification preference.
/// </summary>
public sealed record UpdatePreferenceRequest
{
    /// <summary>Notification type name to configure.</summary>
    public required string NotificationTypeName { get; init; }

    /// <summary>Channel name to configure (e.g. InApp, Email, SignalR).</summary>
    public required string ChannelName { get; init; }

    /// <summary>Whether the channel is enabled for this notification type.</summary>
    public required bool IsEnabled { get; init; }
}
