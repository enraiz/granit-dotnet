namespace Granit.DataExchange.Import.Parsing;

/// <summary>
/// A single raw row from the imported file.
/// All values are strings — type conversion happens in <see cref="Mapping.IDataMapper{TEntity}"/>.
/// </summary>
/// <param name="RowNumber">One-based row number in the source file (for error reporting).</param>
/// <param name="Values">Column name → raw string value dictionary.</param>
public sealed record RawImportRow(
    int RowNumber,
    IReadOnlyDictionary<string, string?> Values);
