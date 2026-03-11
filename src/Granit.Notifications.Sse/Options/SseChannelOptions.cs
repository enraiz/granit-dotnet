namespace Granit.Notifications.Sse.Options;

/// <summary>
/// Configuration options for the SSE notification channel.
/// </summary>
public sealed class SseChannelOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:Sse";

    /// <summary>
    /// Heartbeat interval in seconds. Keeps the connection alive through proxies and load-balancers.
    /// Default is 30 seconds.
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 30;
}
