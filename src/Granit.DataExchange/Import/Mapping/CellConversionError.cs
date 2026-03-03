namespace Granit.DataExchange.Import.Mapping;

/// <summary>
/// Describes a conversion failure for a single cell during data mapping.
/// </summary>
/// <param name="SourceColumn">The source column name.</param>
/// <param name="TargetProperty">The target property that was being mapped to.</param>
/// <param name="RawValue">The raw string value that failed conversion, or <c>null</c>.</param>
/// <param name="ExpectedType">The expected CLR type name.</param>
/// <param name="ErrorCode">A structured error code (e.g. <c>"Granit:DataExchange:InvalidFormat"</c>).</param>
public sealed record CellConversionError(
    string SourceColumn,
    string TargetProperty,
    string? RawValue,
    string ExpectedType,
    string ErrorCode);
