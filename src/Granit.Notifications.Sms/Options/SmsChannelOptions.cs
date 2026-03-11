namespace Granit.Notifications.Sms.Options;

/// <summary>Configuration for the SMS notification channel.</summary>
public sealed class SmsChannelOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:Sms";

    /// <summary>Provider key for Keyed Services resolution (e.g. "Brevo", "Twilio").</summary>
    public string Provider { get; set; } = "Brevo";

    /// <summary>Default sender ID.</summary>
    public string? SenderId { get; set; }
}
