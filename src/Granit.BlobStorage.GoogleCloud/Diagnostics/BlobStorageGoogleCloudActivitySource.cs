using System.Diagnostics;

namespace Granit.BlobStorage.GoogleCloud.Diagnostics;

/// <summary>
/// Central <see cref="ActivitySource"/> for Granit.BlobStorage.GoogleCloud distributed tracing.
/// </summary>
/// <remarks>
/// <c>Granit.Observability</c> adds this source automatically via
/// <c>GranitActivitySourceRegistry</c> when both packages are used.
/// </remarks>
internal static class BlobStorageGoogleCloudActivitySource
{
    /// <summary>The name of the Granit.BlobStorage.GoogleCloud <see cref="ActivitySource"/>.</summary>
    internal const string Name = "Granit.BlobStorage.GoogleCloud";

    /// <summary>The singleton <see cref="ActivitySource"/> instance.</summary>
    internal static readonly ActivitySource Source = new(Name);

    // ──── Operation names ────

    internal const string UploadTicket = "blobstorage.upload-ticket";
    internal const string DownloadUrl = "blobstorage.download-url";
    internal const string Delete = "blobstorage.delete";
    internal const string GetSize = "blobstorage.get-size";
    internal const string PartialStream = "blobstorage.partial-stream";
    internal const string Save = "blobstorage.save";
    internal const string Read = "blobstorage.read";

    // ──── Tag names ────

    internal const string TagBucket = "blobstorage.bucket";
    internal const string TagObjectKey = "blobstorage.object_key";
    internal const string TagContentType = "blobstorage.content_type";
}
