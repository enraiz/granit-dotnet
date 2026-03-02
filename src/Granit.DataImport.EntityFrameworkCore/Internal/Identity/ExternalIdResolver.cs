using Granit.Core.MultiTenancy;
using Granit.DataImport.EntityFrameworkCore.Internal.Entities;
using Granit.DataImport.Identity;
using Granit.DataImport.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataImport.EntityFrameworkCore.Internal.Identity;

/// <summary>
/// Resolves entity identity using an external ID mapping table (Odoo <c>__export__</c> pattern).
/// Looks up the external ID in <see cref="DataImportDbContext"/>, then loads the entity from the application DbContext.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TContext">The application DbContext type.</typeparam>
internal sealed class ExternalIdResolver<TEntity, TContext>(
    IDbContextFactory<DataImportDbContext> importContextFactory,
    IDbContextFactory<TContext> appContextFactory,
    ImportDefinition<TEntity> definition,
    ICurrentTenant currentTenant) : IRecordIdentityResolver<TEntity>
    where TEntity : class
    where TContext : DbContext
{
    /// <inheritdoc/>
    public async Task<RecordIdentity<TEntity>> ResolveAsync(TEntity entity, CancellationToken ct = default)
    {
        // External ID is stored as a special property on the entity
        // The value is extracted from the mapped "ExternalId" column
        string? externalId = ExtractExternalId(entity);
        if (string.IsNullOrEmpty(externalId))
        {
            return new RecordIdentity<TEntity> { Operation = RecordOperation.Insert };
        }

        await using DataImportDbContext importContext = await importContextFactory.CreateDbContextAsync(ct);
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;
        string definitionName = definition.Name;

        ExternalIdMappingEntity? mapping = await importContext.ExternalIdMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.DefinitionName == definitionName
                     && e.ExternalId == externalId
                     && e.TenantId == tenantId,
                ct);

        if (mapping is null)
        {
            return new RecordIdentity<TEntity> { Operation = RecordOperation.Insert };
        }

        await using TContext appContext = await appContextFactory.CreateDbContextAsync(ct);
        TEntity? existing = await appContext.Set<TEntity>().FindAsync([mapping.InternalId], ct);

        if (existing is null)
        {
            return new RecordIdentity<TEntity> { Operation = RecordOperation.Insert };
        }

        return new RecordIdentity<TEntity>
        {
            Operation = RecordOperation.Update,
            ExistingEntity = existing,
        };
    }

    private static string? ExtractExternalId(TEntity entity)
    {
        // The external ID is expected to be in a property named "ExternalId" on the entity,
        // or via the first business key property if configured for external ID mode.
        System.Reflection.PropertyInfo? prop = typeof(TEntity).GetProperty("ExternalId");
        return prop?.GetValue(entity)?.ToString();
    }
}
