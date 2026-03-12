namespace Granit.Notifications.WhatsApp.Options;

/// <summary>Configuration for the WhatsApp notification channel.</summary>
public sealed class WhatsAppChannelOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:WhatsApp";

    /// <summary>Provider key for Keyed Services resolution (e.g. "Brevo", "Twilio"). Required.</summary>
    public string Provider { get; set; } = string.Empty;
}
