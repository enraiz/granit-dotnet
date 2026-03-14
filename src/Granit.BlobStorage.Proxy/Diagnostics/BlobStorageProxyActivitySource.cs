using System.Diagnostics;

namespace Granit.BlobStorage.Proxy.Diagnostics;

/// <summary>
/// Central <see cref="ActivitySource"/> for Granit.BlobStorage.Proxy distributed tracing.
/// </summary>
internal static class BlobStorageProxyActivitySource
{
    /// <summary>The name of the Granit.BlobStorage.Proxy <see cref="ActivitySource"/>.</summary>
    internal const string Name = "Granit.BlobStorage.Proxy";

    /// <summary>The singleton <see cref="ActivitySource"/> instance.</summary>
    internal static readonly ActivitySource Source = new(Name);

    // ──── Operation names ────

    internal const string UploadTicket = "blobproxy.upload-ticket";
    internal const string DownloadUrl = "blobproxy.download-url";
    internal const string Upload = "blobproxy.upload";
    internal const string Download = "blobproxy.download";

    // ──── Tag names ────

    internal const string TagBucket = "blobproxy.bucket";
    internal const string TagObjectKey = "blobproxy.object_key";
    internal const string TagContentType = "blobproxy.content_type";
}
