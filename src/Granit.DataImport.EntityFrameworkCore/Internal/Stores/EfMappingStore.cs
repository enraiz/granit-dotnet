using System.Text.Json;
using Granit.Core.MultiTenancy;
using Granit.DataImport.EntityFrameworkCore.Internal.Entities;
using Granit.DataImport.Mapping;
using Granit.Timing;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataImport.EntityFrameworkCore.Internal.Stores;

/// <summary>
/// EF Core implementation of <see cref="IMappingStore"/>.
/// Persists column mappings per import definition and tenant in <see cref="DataImportDbContext"/>.
/// </summary>
internal sealed class EfMappingStore(
    IDbContextFactory<DataImportDbContext> contextFactory,
    IClock clock,
    ICurrentTenant currentTenant) : IMappingStore
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ColumnMapping>> LoadAsync(
        string definitionName, CancellationToken ct = default)
    {
        await using DataImportDbContext context = await contextFactory.CreateDbContextAsync(ct);
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;

        SavedMappingEntity? entity = await context.SavedMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.DefinitionName == definitionName && e.TenantId == tenantId, ct);

        if (entity is null)
        {
            return [];
        }

        List<ColumnMapping>? mappings = JsonSerializer.Deserialize<List<ColumnMapping>>(entity.MappingsJson);
        return mappings?.AsReadOnly() ?? (IReadOnlyList<ColumnMapping>)[];
    }

    /// <inheritdoc/>
    public async Task SaveAsync(
        string definitionName,
        IReadOnlyList<ColumnMapping> mappings,
        CancellationToken ct = default)
    {
        await using DataImportDbContext context = await contextFactory.CreateDbContextAsync(ct);
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;
        string json = JsonSerializer.Serialize(mappings);

        SavedMappingEntity? existing = await context.SavedMappings
            .FirstOrDefaultAsync(
                e => e.DefinitionName == definitionName && e.TenantId == tenantId, ct);

        if (existing is not null)
        {
            existing.MappingsJson = json;
            existing.SavedAt = clock.Now;
        }
        else
        {
            context.SavedMappings.Add(new SavedMappingEntity
            {
                Id = Guid.NewGuid(),
                DefinitionName = definitionName,
                TenantId = tenantId,
                MappingsJson = json,
                SavedAt = clock.Now,
                SavedBy = "system",
            });
        }

        await context.SaveChangesAsync(ct);
    }
}
