namespace Granit.DataExchange.Import.Mapping;

/// <summary>
/// Maps a source file column to a target entity property.
/// </summary>
/// <param name="SourceColumn">Column name from the imported file.</param>
/// <param name="TargetProperty">
/// Target property path on the entity (e.g. <c>"Email"</c>), or <c>null</c> if the column is unmapped.
/// </param>
/// <param name="Confidence">How the mapping was determined.</param>
public sealed record ImportColumnMapping(
    string SourceColumn,
    string? TargetProperty,
    MappingConfidence Confidence);
