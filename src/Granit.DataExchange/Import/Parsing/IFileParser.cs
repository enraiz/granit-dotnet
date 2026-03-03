namespace Granit.DataExchange.Import.Parsing;

/// <summary>
/// Parses a file stream into raw import rows.
/// Each implementation handles a specific format (CSV, Excel, etc.) and is dispatched
/// by <see cref="CanParse"/> — same pattern as <c>ITemplateEngine.CanRender()</c>.
/// </summary>
public interface IFileParser
{
    /// <summary>
    /// Determines whether this parser can handle the given MIME type.
    /// </summary>
    /// <param name="mimeType">The MIME type of the uploaded file (e.g. <c>"text/csv"</c>).</param>
    /// <returns><c>true</c> if this parser supports the format.</returns>
    bool CanParse(string mimeType);

    /// <summary>
    /// Extracts column headers from the file.
    /// </summary>
    /// <param name="stream">The file stream (seekable).</param>
    /// <param name="options">Parsing options (encoding, separator, sheet, etc.).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An ordered list of column header names.</returns>
    Task<IReadOnlyList<string>> ExtractHeadersAsync(
        Stream stream,
        FileParsingOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Reads a limited number of rows for preview purposes.
    /// </summary>
    /// <param name="stream">The file stream (seekable).</param>
    /// <param name="options">Parsing options.</param>
    /// <param name="maxRows">Maximum number of data rows to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of row arrays, where each array contains the cell values in column order.</returns>
    Task<IReadOnlyList<string[]>> ReadPreviewAsync(
        Stream stream,
        FileParsingOptions options,
        int maxRows = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Parses the entire file as a streaming sequence of raw rows.
    /// Only one row is in memory at a time.
    /// </summary>
    /// <param name="stream">The file stream.</param>
    /// <param name="options">Parsing options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of raw import rows.</returns>
    IAsyncEnumerable<RawImportRow> ParseAsync(
        Stream stream,
        FileParsingOptions options,
        CancellationToken ct = default);
}
