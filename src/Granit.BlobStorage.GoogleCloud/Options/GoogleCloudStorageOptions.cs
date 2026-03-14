using Granit.BlobStorage.Options;
using Microsoft.Extensions.Options;

namespace Granit.BlobStorage.GoogleCloud.Options;

/// <summary>
/// Configuration for the Google Cloud Storage blob storage provider.
/// Extends <see cref="BlobStorageOptions"/> with GCS-specific connection settings.
/// </summary>
/// <remarks>
/// <para>
/// <b>Authentication</b>: by default, the provider uses Application Default Credentials (ADC).
/// When running on GCE/GKE/Cloud Run, ADC is automatic (Workload Identity). For local
/// development, set <c>GOOGLE_APPLICATION_CREDENTIALS</c> or use <c>gcloud auth application-default login</c>.
/// </para>
/// <para>
/// Alternatively, set <see cref="CredentialFilePath"/> to a service account key file.
/// In production, prefer Workload Identity over exported keys. Inject key paths from
/// <c>Granit.Vault</c>; never hardcode paths in <c>appsettings.json</c>.
/// </para>
/// </remarks>
public sealed class GoogleCloudStorageOptions : BlobStorageOptions
{
    /// <summary>
    /// GCP project ID. Required for bucket operations and signed URL generation.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Default GCS bucket name when using <see cref="BlobTenantIsolation.Prefix"/> strategy.
    /// </summary>
    public string DefaultBucket { get; set; } = string.Empty;

    /// <summary>
    /// Optional path to a service account key JSON file.
    /// When <c>null</c> or empty, Application Default Credentials (ADC) are used.
    /// </summary>
    public string? CredentialFilePath { get; set; }

    /// <summary>
    /// Multi-tenant isolation strategy. Defaults to <see cref="BlobTenantIsolation.Prefix"/>.
    /// </summary>
    public BlobTenantIsolation TenantIsolation { get; set; } = BlobTenantIsolation.Prefix;
}

/// <summary>
/// Validates <see cref="GoogleCloudStorageOptions"/> at startup (fail-fast on missing configuration).
/// </summary>
internal sealed class GoogleCloudStorageOptionsValidator : IValidateOptions<GoogleCloudStorageOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, GoogleCloudStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ProjectId))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.ProjectId)} must be non-empty. " +
                "Set it to your GCP project ID (e.g. my-project-123).");
        }

        if (string.IsNullOrWhiteSpace(options.DefaultBucket))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.DefaultBucket)} must be non-empty.");
        }

        return ValidateOptionsResult.Success;
    }
}
