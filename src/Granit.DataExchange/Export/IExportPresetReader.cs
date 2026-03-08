namespace Granit.DataExchange.Export;

/// <summary>
/// Reads saved export presets.
/// </summary>
public interface IExportPresetReader
{
    /// <summary>
    /// Gets a saved preset by definition name and preset name.
    /// </summary>
    Task<ExportPreset?> GetAsync(string definitionName, string presetName, CancellationToken ct = default);

    /// <summary>
    /// Lists all saved presets for a given export definition.
    /// </summary>
    Task<IReadOnlyList<ExportPreset>> ListAsync(string definitionName, CancellationToken ct = default);
}
