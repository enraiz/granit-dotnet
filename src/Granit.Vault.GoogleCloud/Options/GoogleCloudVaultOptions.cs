using System.ComponentModel.DataAnnotations;

namespace Granit.Vault.GoogleCloud.Options;

/// <summary>Configuration for Google Cloud KMS and Secret Manager.</summary>
public sealed class GoogleCloudVaultOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Vault:GoogleCloud";

    /// <summary>GCP project ID. Required.</summary>
    [Required]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Cloud KMS location (e.g. "europe-west1", "global"). Required.</summary>
    [Required]
    public string Location { get; set; } = "global";

    /// <summary>Cloud KMS key ring name. Required.</summary>
    [Required]
    public string KeyRing { get; set; } = string.Empty;

    /// <summary>Cloud KMS crypto key name for transit encryption. Required.</summary>
    [Required]
    public string CryptoKey { get; set; } = string.Empty;

    /// <summary>Secret Manager secret name for database credentials. Optional.</summary>
    public string? DatabaseSecretName { get; set; }

    /// <summary>Interval in minutes between rotation checks for database credentials. Default: 5.</summary>
    [Range(1, 1440)]
    public double RotationCheckIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Optional path to a service account key JSON file.
    /// When <c>null</c>, Application Default Credentials (ADC) are used.
    /// </summary>
    public string? CredentialFilePath { get; set; }

    /// <summary>API call timeout in seconds. Default: 30.</summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}
