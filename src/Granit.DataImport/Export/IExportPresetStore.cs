namespace Granit.DataImport.Export;

/// <summary>
/// Stores and retrieves saved export presets (Odoo export template pattern).
/// </summary>
/// <remarks>
/// The default registration is a null-object (<see cref="Internal.NullExportPresetStore"/>)
/// that returns empty results. Install <c>Granit.DataImport.EntityFrameworkCore</c>
/// to get EF Core-backed persistence with multi-tenant isolation.
/// </remarks>
public interface IExportPresetStore
{
    /// <summary>
    /// Gets a saved preset by definition name and preset name.
    /// </summary>
    Task<ExportPreset?> GetAsync(string definitionName, string presetName, CancellationToken ct = default);

    /// <summary>
    /// Lists all saved presets for a given export definition.
    /// </summary>
    Task<IReadOnlyList<ExportPreset>> ListAsync(string definitionName, CancellationToken ct = default);

    /// <summary>
    /// Saves or updates a preset (upsert by definition name + preset name).
    /// </summary>
    Task SaveAsync(ExportPreset preset, CancellationToken ct = default);

    /// <summary>
    /// Deletes a saved preset.
    /// </summary>
    Task DeleteAsync(string definitionName, string presetName, CancellationToken ct = default);
}
