namespace Granit.DataExchange.Import.Pipeline;

/// <summary>
/// Provides file streams for import jobs.
/// The application registers an implementation that retrieves the file from its blob storage.
/// </summary>
/// <remarks>
/// A concrete implementation is typically registered in the host application,
/// bridging <c>IBlobStorage</c> or a local file system to the import pipeline.
/// </remarks>
public interface IImportFileProvider
{
    /// <summary>
    /// Opens a readable stream for the given blob reference.
    /// </summary>
    /// <param name="blobReference">The blob reference stored on <see cref="Domain.ImportJob.BlobReference"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A readable stream. The caller is responsible for disposing it.</returns>
    Task<Stream> OpenAsync(string blobReference, CancellationToken ct = default);

    /// <summary>
    /// Saves a file and returns a blob reference for subsequent retrieval via <see cref="OpenAsync"/>.
    /// </summary>
    /// <param name="fileName">The original file name.</param>
    /// <param name="content">The file stream to save.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The blob reference string.</returns>
    Task<string> SaveAsync(string fileName, Stream content, CancellationToken ct = default);

    /// <summary>
    /// Deletes a previously saved file by its blob reference.
    /// </summary>
    /// <param name="blobReference">The blob reference to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string blobReference, CancellationToken ct = default);
}
