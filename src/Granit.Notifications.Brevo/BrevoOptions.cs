namespace Granit.Notifications.Brevo;

/// <summary>Brevo API configuration options.</summary>
public sealed class BrevoOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:Brevo";

    /// <summary>Brevo API key (should come from Vault).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Default sender email address for transactional emails.</summary>
    public string DefaultSenderEmail { get; set; } = string.Empty;

    /// <summary>Default sender display name for transactional emails.</summary>
    public string DefaultSenderName { get; set; } = string.Empty;

    /// <summary>Default SMS sender ID displayed on the phone.</summary>
    public string DefaultSmsSenderId { get; set; } = string.Empty;

    /// <summary>Brevo API base URL.</summary>
    public string BaseUrl { get; set; } = "https://api.brevo.com/v3";
}
