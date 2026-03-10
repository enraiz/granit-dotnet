using System.ComponentModel.DataAnnotations;

namespace Granit.Notifications.Zulip.Options;

/// <summary>Configuration for the Zulip Bot API connection.</summary>
public sealed class ZulipBotOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:Zulip:Bot";

    /// <summary>Zulip server base URL (e.g. "https://zulip.example.com").</summary>
    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Bot email address.</summary>
    [Required]
    public string BotEmail { get; set; } = string.Empty;

    /// <summary>Bot API key (resolved from Vault at runtime).</summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
