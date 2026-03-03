namespace Granit.DataImport.Endpoints.Dtos;

/// <summary>
/// Request DTO for saving an export preset.
/// </summary>
/// <param name="DefinitionName">The export definition name.</param>
/// <param name="PresetName">User-facing preset name.</param>
/// <param name="SelectedFields">Ordered list of field property paths.</param>
/// <param name="Format">Output format (<c>"xlsx"</c> or <c>"csv"</c>).</param>
/// <param name="IncludeIdForImport">Whether to include the entity ID for roundtrip import.</param>
public sealed record SaveExportPresetRequest(
    string DefinitionName,
    string PresetName,
    IReadOnlyList<string> SelectedFields,
    string Format,
    bool IncludeIdForImport);
