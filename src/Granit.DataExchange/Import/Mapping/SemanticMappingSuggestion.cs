namespace Granit.DataExchange.Import.Mapping;

/// <summary>
/// A mapping suggestion from the AI semantic matching service.
/// </summary>
/// <param name="SourceColumn">Column header from the imported file.</param>
/// <param name="TargetProperty">Suggested target property path.</param>
/// <param name="Score">Confidence score (0.0 to 1.0).</param>
public sealed record SemanticMappingSuggestion(
    string SourceColumn,
    string TargetProperty,
    double Score);
