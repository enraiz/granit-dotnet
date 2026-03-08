namespace Granit.DataExchange.Export.Internal;

/// <summary>
/// Null-object implementation of <see cref="IExportPresetReader"/> and <see cref="IExportPresetWriter"/>.
/// Returns empty results. Replaced by EF Core implementation when
/// <c>Granit.DataExchange.EntityFrameworkCore</c> is installed.
/// </summary>
internal sealed class NullExportPresetStore : IExportPresetReader, IExportPresetWriter
{
    public Task<ExportPreset?> GetAsync(
        string definitionName, string presetName, CancellationToken cancellationToken = default) =>
        Task.FromResult<ExportPreset?>(null);

    public Task<IReadOnlyList<ExportPreset>> ListAsync(
        string definitionName, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ExportPreset>>([]);

    public Task SaveAsync(ExportPreset preset, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteAsync(
        string definitionName, string presetName, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
