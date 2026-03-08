using System.Text.Json;

namespace Granit.Notifications.MobilePush;

/// <summary>Mobile push notification message to send to device(s).</summary>
public sealed record MobilePushMessage
{
    /// <summary>Device tokens to target.</summary>
    public required IReadOnlyList<string> DeviceTokens { get; init; }

    /// <summary>Notification title displayed in the system tray.</summary>
    public required string Title { get; init; }

    /// <summary>Notification body text.</summary>
    public required string Body { get; init; }

    /// <summary>
    /// Optional data payload (key/value). Must NOT contain PII or health data —
    /// the push serves as a wake-up signal only (HDS compliance).
    /// </summary>
    public JsonElement? Data { get; init; }
}
