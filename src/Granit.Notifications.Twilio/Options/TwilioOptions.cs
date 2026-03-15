using System.ComponentModel.DataAnnotations;

namespace Granit.Notifications.Twilio.Options;

/// <summary>Twilio Messaging API configuration options.</summary>
public sealed class TwilioOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:Twilio";

    /// <summary>Twilio Account SID (should come from Vault).</summary>
    [Required]
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>Twilio Auth Token (should come from Vault).</summary>
    [Required]
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>Default sender phone number for SMS in E.164 format (e.g. +1234567890).</summary>
    [Required]
    public string DefaultSmsFromNumber { get; set; } = string.Empty;

    /// <summary>
    /// Default sender phone number for WhatsApp in E.164 format.
    /// Falls back to <see cref="DefaultSmsFromNumber"/> when <see langword="null"/>.
    /// The <c>whatsapp:</c> prefix is added programmatically.
    /// </summary>
    public string? DefaultWhatsAppFromNumber { get; set; }

    /// <summary>Twilio API base URL.</summary>
    [Required]
    public string BaseUrl { get; set; } = "https://api.twilio.com";

    /// <summary>HTTP request timeout in seconds. Default: 30.</summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}
