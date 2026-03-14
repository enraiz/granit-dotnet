using System.ComponentModel.DataAnnotations;

namespace Granit.Vault.Aws.Options;

/// <summary>Configuration for AWS KMS and Secrets Manager.</summary>
public sealed class AwsVaultOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Vault:Aws";

    /// <summary>AWS region endpoint (e.g. "eu-west-1"). Required.</summary>
    [Required]
    public string Region { get; set; } = string.Empty;

    /// <summary>KMS key ARN or alias for transit encryption (e.g. "alias/granit-encryption"). Required.</summary>
    [Required]
    public string KmsKeyId { get; set; } = string.Empty;

    /// <summary>Secrets Manager ARN or name for database credentials. Optional.</summary>
    public string? DatabaseSecretArn { get; set; }

    /// <summary>Interval in minutes between rotation checks for database credentials. Default: 5.</summary>
    [Range(1, 1440)]
    public double RotationCheckIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// AWS access key ID. When <c>null</c>, the SDK uses the default credential chain
    /// (IAM roles on ECS/EKS, environment variables, or shared credentials file).
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// AWS secret access key. When <c>null</c>, the SDK uses the default credential chain.
    /// </summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>API call timeout in seconds. Default: 30.</summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}
