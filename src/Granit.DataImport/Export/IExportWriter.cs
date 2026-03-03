namespace Granit.DataImport.Export;

/// <summary>
/// Writes tabular export data to a specific file format (Excel, CSV, etc.).
/// </summary>
/// <remarks>
/// <para>
/// Implementations are registered as singletons via the format-specific packages
/// (<c>Granit.DataImport.Excel</c>, <c>Granit.DataImport.Csv</c>).
/// </para>
/// <para>
/// The writer receives a stream of flattened row dictionaries (property path → value)
/// and writes them sequentially. For large datasets, the writer should process rows
/// in a streaming fashion to minimize memory usage.
/// </para>
/// </remarks>
public interface IExportWriter
{
    /// <summary>
    /// Whether this writer supports the given format.
    /// </summary>
    /// <param name="format">The format identifier (e.g. <c>"xlsx"</c>, <c>"csv"</c>).</param>
    bool CanWrite(string format);

    /// <summary>
    /// MIME type for the output file (e.g. <c>"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"</c>).
    /// </summary>
    string MimeType { get; }

    /// <summary>
    /// File extension including the dot (e.g. <c>".xlsx"</c>, <c>".csv"</c>).
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Writes export data to the output stream.
    /// </summary>
    /// <param name="output">The target stream.</param>
    /// <param name="fields">Ordered field descriptors (defines columns).</param>
    /// <param name="rows">Streaming row data (property path → value).</param>
    /// <param name="ct">Cancellation token.</param>
    Task WriteAsync(
        Stream output,
        IReadOnlyList<ExportFieldDescriptor> fields,
        IAsyncEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct = default);
}
