namespace Granit.BlobStorage.Internal;

/// <summary>
/// Low-level blob storage operations. Implemented by every provider package
/// (S3, Azure Blob, FileSystem, Database).
/// </summary>
internal interface IBlobStoreProvider
{
    /// <summary>Writes blob content to the store.</summary>
    /// <param name="bucket">Physical bucket / base path resolved by <see cref="IBlobKeyStrategy"/>.</param>
    /// <param name="objectKey">Full object key including tenant prefix.</param>
    /// <param name="content">
    /// Blob content stream. Implementations MUST stream end-to-end without
    /// buffering the entire payload in memory (critical for large files).
    /// The caller retains ownership of the stream.
    /// </param>
    /// <param name="contentType">MIME content type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(
        string bucket,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Opens the full blob content for reading.</summary>
    /// <returns>
    /// A readable <see cref="Stream"/>. The caller is responsible for disposing it.
    /// Implementations MUST return a streaming response, not a fully buffered copy.
    /// </returns>
    Task<Stream> OpenReadAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default);

    /// <summary>Physically deletes the blob content from the store.</summary>
    Task DeleteAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the actual size of the blob in bytes.</summary>
    Task<long> GetSizeAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a partial read stream starting from byte 0 up to <paramref name="byteCount"/> bytes.
    /// Used by <c>MagicBytesValidator</c> for content-type detection without buffering the full file.
    /// The caller is responsible for disposing the returned stream.
    /// </summary>
    Task<Stream> OpenPartialReadAsync(
        string bucket,
        string objectKey,
        int byteCount,
        CancellationToken cancellationToken = default);
}
