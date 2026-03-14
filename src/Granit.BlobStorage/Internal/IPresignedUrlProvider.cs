using Granit.BlobStorage.Options;

namespace Granit.BlobStorage.Internal;

/// <summary>
/// Generates pre-signed URLs for direct client-to-storage transfers.
/// Implemented by cloud providers (S3, Azure Blob Storage).
/// For server-side providers (FileSystem, Database), <c>Granit.BlobStorage.Proxy</c>
/// provides a proxy-based implementation.
/// </summary>
internal interface IPresignedUrlProvider
{
    /// <summary>
    /// Generates a pre-signed PUT URL and returns the full <see cref="PresignedUploadTicket"/>.
    /// </summary>
    Task<PresignedUploadTicket> GenerateUploadTicketAsync(
        string bucket,
        string objectKey,
        Guid blobId,
        BlobUploadRequest request,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a pre-signed GET URL for client download.
    /// </summary>
    Task<PresignedDownloadUrl> GenerateDownloadUrlAsync(
        string bucket,
        string objectKey,
        DownloadUrlOptions? options,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);
}
