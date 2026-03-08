namespace Granit.Notifications.MobilePush;

/// <summary>Configuration for the mobile push notification channel.</summary>
public sealed class MobilePushChannelOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:MobilePush";

    /// <summary>Provider key for Keyed Services resolution (e.g. "Fcm").</summary>
    public string Provider { get; set; } = "Fcm";
}
