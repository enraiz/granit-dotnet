namespace Granit.Notifications;

/// <summary>
/// Configuration options for the Granit.Notifications engine.
/// </summary>
public sealed class NotificationsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications";

    /// <summary>Maximum parallel delivery messages processed concurrently.</summary>
    public int MaxParallelDeliveries { get; set; } = 8;
}
