using System.Text.Json;
using Granit.Core.MultiTenancy;
using Granit.DataExchange.EntityFrameworkCore.Internal.Export.Entities;
using Granit.DataExchange.EntityFrameworkCore.Internal.Import.Entities;
using Granit.DataExchange.Export;
using Granit.Timing;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataExchange.EntityFrameworkCore.Internal.Export.Stores;

/// <summary>
/// EF Core implementation of <see cref="IExportPresetReader"/> and <see cref="IExportPresetWriter"/>.
/// Persists export presets per definition, preset name, and tenant in <see cref="DataExchangeDbContext"/>.
/// </summary>
internal sealed class EfExportPresetStore(
    IDbContextFactory<DataExchangeDbContext> contextFactory,
    IClock clock,
    ICurrentTenant currentTenant) : IExportPresetReader, IExportPresetWriter
{
    /// <inheritdoc/>
    public async Task<ExportPreset?> GetAsync(
        string definitionName, string presetName, CancellationToken ct = default)
    {
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;

        ExportPresetEntity? entity = await context.ExportPresets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.DefinitionName == definitionName
                     && e.PresetName == presetName
                     && e.TenantId == tenantId, ct).ConfigureAwait(false);

        return entity is null ? null : ToPreset(entity);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExportPreset>> ListAsync(
        string definitionName, CancellationToken ct = default)
    {
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;

        List<ExportPresetEntity> entities = await context.ExportPresets
            .AsNoTracking()
            .Where(e => e.DefinitionName == definitionName && e.TenantId == tenantId)
            .OrderBy(e => e.PresetName)
            .ToListAsync(ct).ConfigureAwait(false);

        return entities.Select(ToPreset).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task SaveAsync(ExportPreset preset, CancellationToken ct = default)
    {
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;
        string fieldsJson = JsonSerializer.Serialize(preset.SelectedFields);

        ExportPresetEntity? existing = await context.ExportPresets
            .FirstOrDefaultAsync(
                e => e.DefinitionName == preset.DefinitionName
                     && e.PresetName == preset.PresetName
                     && e.TenantId == tenantId, ct).ConfigureAwait(false);

        if (existing is not null)
        {
            existing.FieldsJson = fieldsJson;
            existing.Format = preset.Format;
            existing.IncludeIdForImport = preset.IncludeIdForImport;
            existing.SavedAt = clock.Now;
        }
        else
        {
            context.ExportPresets.Add(new ExportPresetEntity
            {
                Id = Guid.NewGuid(),
                DefinitionName = preset.DefinitionName,
                PresetName = preset.PresetName,
                TenantId = tenantId,
                FieldsJson = fieldsJson,
                Format = preset.Format,
                IncludeIdForImport = preset.IncludeIdForImport,
                SavedAt = clock.Now,
                SavedBy = "system",
            });
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        string definitionName, string presetName, CancellationToken ct = default)
    {
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;

        ExportPresetEntity? entity = await context.ExportPresets
            .FirstOrDefaultAsync(
                e => e.DefinitionName == definitionName
                     && e.PresetName == presetName
                     && e.TenantId == tenantId, ct).ConfigureAwait(false);

        if (entity is not null)
        {
            context.ExportPresets.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private static ExportPreset ToPreset(ExportPresetEntity entity)
    {
        List<string>? fields = JsonSerializer.Deserialize<List<string>>(entity.FieldsJson);
        return new ExportPreset(
            entity.DefinitionName,
            entity.PresetName,
            fields?.AsReadOnly() ?? (IReadOnlyList<string>)[],
            entity.Format,
            entity.IncludeIdForImport);
    }
}
