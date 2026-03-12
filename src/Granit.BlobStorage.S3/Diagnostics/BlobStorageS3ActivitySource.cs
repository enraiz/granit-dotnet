using System.Diagnostics;

namespace Granit.BlobStorage.S3.Diagnostics;

/// <summary>
/// Central <see cref="ActivitySource"/> for Granit.BlobStorage.S3 distributed tracing.
/// </summary>
/// <remarks>
/// <c>Granit.Observability</c> adds this source automatically via
/// <c>GranitActivitySourceRegistry</c> when both packages are used.
/// </remarks>
internal static class BlobStorageS3ActivitySource
{
    /// <summary>The name of the Granit.BlobStorage.S3 <see cref="ActivitySource"/>.</summary>
    internal const string Name = "Granit.BlobStorage.S3";

    /// <summary>The singleton <see cref="ActivitySource"/> instance.</summary>
    internal static readonly ActivitySource Source = new(Name);

    // ──── Operation names ────

    internal const string UploadTicket = "blobstorage.upload-ticket";
    internal const string DownloadUrl = "blobstorage.download-url";
    internal const string Delete = "blobstorage.delete";
    internal const string GetSize = "blobstorage.get-size";
    internal const string PartialStream = "blobstorage.partial-stream";

    // ──── Tag names ────

    internal const string TagBucket = "blobstorage.bucket";
    internal const string TagObjectKey = "blobstorage.object_key";
    internal const string TagContentType = "blobstorage.content_type";
}
