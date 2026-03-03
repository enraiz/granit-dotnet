using Granit.DataExchange.Export;

namespace Granit.DataExchange.Endpoints.Dtos.Export;

/// <summary>
/// Response DTO for a saved export preset.
/// </summary>
public sealed record ExportPresetResponse(
    string DefinitionName,
    string PresetName,
    IReadOnlyList<string> SelectedFields,
    string Format,
    bool IncludeIdForImport)
{
    /// <summary>
    /// Maps an <see cref="ExportPreset"/> to a response DTO.
    /// </summary>
    internal static ExportPresetResponse FromPreset(ExportPreset preset) =>
        new(preset.DefinitionName, preset.PresetName, preset.SelectedFields,
            preset.Format, preset.IncludeIdForImport);
}
