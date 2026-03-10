namespace Granit.BlobStorage.Options;

/// <summary>
/// Core options shared by all blob storage providers.
/// Provider-specific options (e.g. <c>S3BlobOptions</c> in <c>Granit.BlobStorage.S3</c>) extend this class.
/// </summary>
public class BlobStorageOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "BlobStorage";

    /// <summary>TTL for Pre-signed upload URLs. Defaults to 15 minutes.</summary>
    public TimeSpan UploadUrlExpiry { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>TTL for Pre-signed download URLs. Defaults to 5 minutes.</summary>
    public TimeSpan DownloadUrlExpiry { get; set; } = TimeSpan.FromMinutes(5);
}
