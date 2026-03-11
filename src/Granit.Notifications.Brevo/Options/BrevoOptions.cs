using System.ComponentModel.DataAnnotations;

namespace Granit.Notifications.Brevo.Options;

/// <summary>Brevo API configuration options.</summary>
public sealed class BrevoOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:Brevo";

    /// <summary>Brevo API key (should come from Vault).</summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Default sender email address for transactional emails.</summary>
    [Required]
    public string DefaultSenderEmail { get; set; } = string.Empty;

    /// <summary>Default sender display name for transactional emails.</summary>
    public string DefaultSenderName { get; set; } = string.Empty;

    /// <summary>Default SMS sender ID displayed on the phone.</summary>
    public string DefaultSmsSenderId { get; set; } = string.Empty;

    /// <summary>Brevo API base URL.</summary>
    [Required]
    public string BaseUrl { get; set; } = "https://api.brevo.com/v3";

    /// <summary>HTTP request timeout in seconds. Default: 30.</summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}
