namespace Granit.DataExchange.Export;

/// <summary>
/// Persists and deletes export presets.
/// </summary>
public interface IExportPresetWriter
{
    /// <summary>
    /// Saves or updates a preset (upsert by definition name + preset name).
    /// </summary>
    Task SaveAsync(ExportPreset preset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a saved preset.
    /// </summary>
    Task DeleteAsync(string definitionName, string presetName, CancellationToken cancellationToken = default);
}
