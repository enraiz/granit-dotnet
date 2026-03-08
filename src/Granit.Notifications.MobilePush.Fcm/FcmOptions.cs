using System.ComponentModel.DataAnnotations;

namespace Granit.Notifications.MobilePush.Fcm;

/// <summary>Configuration for Firebase Cloud Messaging.</summary>
public sealed class FcmOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:MobilePush:Fcm";

    /// <summary>Firebase project ID.</summary>
    [Required]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Service account JSON key (resolved from Vault at runtime).
    /// Used to obtain OAuth 2.0 access tokens for FCM HTTP v1 API.
    /// </summary>
    [Required]
    public string ServiceAccountJson { get; set; } = string.Empty;

    /// <summary>Request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
