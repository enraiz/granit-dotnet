using Granit.DataExchange.Import.Parsing;

namespace Granit.DataExchange.Import.Reporting;

/// <summary>
/// Generates a correction file from the original import file and the import report.
/// The correction file contains only the failed rows, with an extra column indicating the error.
/// </summary>
/// <remarks>
/// Generated on demand (GET <c>/{jobId}/correction-file</c>), not pre-computed.
/// The original file is re-read from blob storage.
/// </remarks>
public interface ICorrectionFileGenerator
{
    /// <summary>
    /// Generates a correction file stream.
    /// </summary>
    /// <param name="originalFileStream">The original uploaded file stream.</param>
    /// <param name="mimeType">The MIME type of the original file.</param>
    /// <param name="report">The import report containing row errors.</param>
    /// <param name="parsingOptions">The parsing options used for the original file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A stream containing the correction file.</returns>
    Task<Stream> GenerateAsync(
        Stream originalFileStream,
        string mimeType,
        ImportReport report,
        FileParsingOptions parsingOptions,
        CancellationToken ct = default);
}
