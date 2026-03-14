using System.ComponentModel.DataAnnotations;

namespace Granit.Notifications.Email.Ses.Options;

/// <summary>Amazon SES connection options.</summary>
public sealed class SesOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Notifications:Ses";

    /// <summary>AWS region endpoint (e.g. "eu-west-1"). Required.</summary>
    [Required]
    public string Region { get; set; } = string.Empty;

    /// <summary>Default sender email address (must be verified in SES).</summary>
    public string? FromAddress { get; set; }

    /// <summary>Optional SES configuration set name for tracking (bounces, complaints).</summary>
    public string? ConfigurationSetName { get; set; }

    /// <summary>
    /// AWS access key ID. When <c>null</c>, the SDK uses the default credential chain
    /// (IAM roles on ECS/EKS, environment variables, or shared credentials file).
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// AWS secret access key. When <c>null</c>, the SDK uses the default credential chain.
    /// </summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>Send timeout in seconds. Default: 30.</summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}
