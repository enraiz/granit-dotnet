namespace Granit.DataImport.Parsing;

/// <summary>
/// Options controlling how a file is parsed by <see cref="IFileParser"/>.
/// </summary>
public sealed class FileParsingOptions
{
    /// <summary>
    /// Character encoding name (e.g. <c>"utf-8"</c>, <c>"windows-1252"</c>).
    /// When <c>null</c>, the parser auto-detects or defaults to UTF-8.
    /// </summary>
    public string? Encoding { get; init; }

    /// <summary>
    /// Column separator character for CSV files.
    /// Default: <c>","</c>.
    /// </summary>
    public string Separator { get; init; } = ",";

    /// <summary>
    /// Quote character for CSV fields.
    /// Default: <c>"\"</c>.
    /// </summary>
    public string QuoteChar { get; init; } = "\"";

    /// <summary>
    /// Number of initial rows to skip before reading headers.
    /// Useful when the file has a title row or metadata before the actual data.
    /// </summary>
    public int SkipRows { get; init; }

    /// <summary>
    /// Zero-based index of the row containing column headers.
    /// Applied after <see cref="SkipRows"/>.
    /// Default: <c>0</c> (first row after skip).
    /// </summary>
    public int HeaderRowIndex { get; init; }

    /// <summary>
    /// Name of the Excel worksheet to read. When <c>null</c>, the first sheet is used.
    /// Ignored for CSV files.
    /// </summary>
    public string? SheetName { get; init; }

    /// <summary>
    /// Date format hint for parsing date/time values (e.g. <c>"dd/MM/yyyy"</c>).
    /// When <c>null</c>, the parser uses the invariant culture defaults.
    /// </summary>
    public string? DateFormat { get; init; }

    /// <summary>
    /// MIME type of the file being parsed (e.g. <c>"text/csv"</c>,
    /// <c>"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"</c>).
    /// Used by parsers that need to distinguish between sub-formats (e.g. <c>.xlsx</c> vs <c>.xls</c>).
    /// </summary>
    public string? MimeType { get; init; }
}
