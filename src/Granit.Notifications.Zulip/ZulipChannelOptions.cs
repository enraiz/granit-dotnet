namespace Granit.Notifications.Zulip;

/// <summary>Configuration for the Zulip notification channel.</summary>
public sealed class ZulipChannelOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:Zulip";

    /// <summary>Default stream for notifications.</summary>
    public string DefaultStream { get; set; } = "alerts";

    /// <summary>Default topic for notifications.</summary>
    public string DefaultTopic { get; set; } = "system";
}
