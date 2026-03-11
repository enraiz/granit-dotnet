namespace Granit.Notifications.Email.Options;

/// <summary>Configuration for the email notification channel.</summary>
public sealed class EmailChannelOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:Email";

    /// <summary>Provider key for Keyed Services resolution (e.g. "Smtp", "Brevo").</summary>
    public string Provider { get; set; } = "Smtp";

    /// <summary>Default sender email address.</summary>
    public string SenderAddress { get; set; } = string.Empty;

    /// <summary>Default sender display name.</summary>
    public string SenderName { get; set; } = string.Empty;
}
