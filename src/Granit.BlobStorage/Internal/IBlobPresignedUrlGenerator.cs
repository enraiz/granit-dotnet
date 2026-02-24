namespace Granit.BlobStorage.Internal;

/// <summary>
/// Generates Pre-signed S3 URLs. Implemented by <c>Granit.BlobStorage.S3</c>.
/// </summary>
internal interface IBlobPresignedUrlGenerator
{
    /// <summary>
    /// Generates a Pre-signed PUT URL and returns the full <see cref="PresignedUploadTicket"/>.
    /// </summary>
    Task<PresignedUploadTicket> GenerateUploadTicketAsync(
        string bucket,
        string objectKey,
        Guid blobId,
        BlobUploadRequest request,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a Pre-signed GET URL for client download.
    /// </summary>
    Task<PresignedDownloadUrl> GenerateDownloadUrlAsync(
        string bucket,
        string objectKey,
        DownloadUrlOptions? options,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);
}
