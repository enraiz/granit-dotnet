namespace Granit.BlobStorage;

/// <summary>
/// Short-lived Pre-signed URL for direct client-to-S3 download.
/// </summary>
/// <param name="Url">Pre-signed S3 GET URL; valid until <see cref="ExpiresAt"/>.</param>
/// <param name="ExpiresAt">UTC expiry of the pre-signed URL.</param>
public sealed record PresignedDownloadUrl(Uri Url, DateTimeOffset ExpiresAt);
