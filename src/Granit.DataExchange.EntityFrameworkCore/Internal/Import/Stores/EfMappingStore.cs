using System.Text.Json;
using Granit.Core.MultiTenancy;
using Granit.DataExchange.EntityFrameworkCore.Internal.Export.Entities;
using Granit.DataExchange.EntityFrameworkCore.Internal.Import.Entities;
using Granit.DataExchange.Import.Mapping;
using Granit.Timing;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataExchange.EntityFrameworkCore.Internal.Import.Stores;

/// <summary>
/// EF Core implementation of <see cref="IMappingStore"/>.
/// Persists column mappings per import definition and tenant in <see cref="DataExchangeDbContext"/>.
/// </summary>
internal sealed class EfMappingStore(
    IDbContextFactory<DataExchangeDbContext> contextFactory,
    IClock clock,
    ICurrentTenant currentTenant) : IMappingStore
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ColumnMapping>> LoadAsync(
        string definitionName, CancellationToken ct = default)
    {
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;

        SavedMappingEntity? entity = await context.SavedMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.DefinitionName == definitionName && e.TenantId == tenantId, ct).ConfigureAwait(false);

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
        await using DataExchangeDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;
        string json = JsonSerializer.Serialize(mappings);

        SavedMappingEntity? existing = await context.SavedMappings
            .FirstOrDefaultAsync(
                e => e.DefinitionName == definitionName && e.TenantId == tenantId, ct).ConfigureAwait(false);

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

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
