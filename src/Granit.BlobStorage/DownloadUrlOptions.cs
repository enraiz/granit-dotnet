namespace Granit.BlobStorage;

/// <summary>
/// Optional parameters for <see cref="IBlobStorage.CreateDownloadUrlAsync"/>.
/// </summary>
/// <param name="Expiry">
/// Override for the default download URL TTL configured in <c>BlobStorageOptions</c>.
/// If <c>null</c>, the configured default is used.
/// </param>
/// <param name="DownloadFileName">
/// When set, injects a <c>Content-Disposition: attachment; filename="..."</c> header
/// into the Pre-signed URL, forcing the browser to download rather than preview.
/// </param>
public sealed record DownloadUrlOptions(
    TimeSpan? Expiry = null,
    string? DownloadFileName = null);
