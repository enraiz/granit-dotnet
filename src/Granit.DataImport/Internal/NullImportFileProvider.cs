using Granit.DataImport.Pipeline;

namespace Granit.DataImport.Internal;

/// <summary>
/// Default implementation of <see cref="IImportFileProvider"/>.
/// Throws <see cref="NotImplementedException"/> — the application must register a concrete implementation.
/// </summary>
internal sealed class NullImportFileProvider : IImportFileProvider
{
    /// <inheritdoc/>
    public Task<Stream> OpenAsync(string blobReference, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "No IImportFileProvider is registered. " +
            "The host application must register an implementation that retrieves files from blob storage.");
}
