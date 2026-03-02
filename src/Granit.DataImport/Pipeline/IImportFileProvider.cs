namespace Granit.DataImport.Pipeline;

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
}
