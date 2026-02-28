namespace Granit.Notifications.SignalR;

/// <summary>
/// Configuration options for the SignalR notification channel.
/// </summary>
public sealed class SignalRChannelOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:SignalR";

    /// <summary>Redis connection string for the SignalR backplane (Kubernetes multi-pod).</summary>
    public string? RedisConnectionString { get; set; }
}
