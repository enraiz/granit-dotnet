namespace Granit.BlobStorage.Internal;

/// <summary>
/// Low-level S3 object operations (non-URL). Implemented by <c>Granit.BlobStorage.S3</c>.
/// </summary>
internal interface IBlobObjectClient
{
    /// <summary>Physically deletes an object from S3.</summary>
    Task DeleteObjectAsync(string bucket, string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the actual size of an S3 object in bytes via a HEAD request.
    /// </summary>
    Task<long> GetObjectSizeBytesAsync(string bucket, string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a partial read stream starting from byte 0 up to <paramref name="byteCount"/> bytes (range GET).
    /// Used by <c>MagicBytesValidator</c> for magic-bytes analysis without buffering the full file.
    /// The caller is responsible for disposing the returned stream.
    /// </summary>
    Task<Stream> OpenPartialStreamAsync(string bucket, string objectKey, int byteCount, CancellationToken cancellationToken = default);
}
