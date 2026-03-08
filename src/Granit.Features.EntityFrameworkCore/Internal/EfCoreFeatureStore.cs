using Granit.Features.Store;
using Microsoft.EntityFrameworkCore;

namespace Granit.Features.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="IFeatureStoreReader"/> and <see cref="IFeatureStoreWriter"/>.
/// Persists tenant-level feature overrides in the <c>feature_overrides</c> table
/// with full HDS audit trail.
/// </summary>
/// <remarks>
/// Registered as the replacement for <c>InMemoryFeatureStore</c> when
/// <c>AddGranitFeaturesEntityFrameworkCore</c> is called.
/// Each operation creates and disposes its own <see cref="GranitFeaturesDbContext"/> via
/// <see cref="IDbContextFactory{TContext}"/>, making it safe for concurrent request handling.
/// </remarks>
internal sealed class EfCoreFeatureStore(
    IDbContextFactory<GranitFeaturesDbContext> contextFactory) : IFeatureStoreReader, IFeatureStoreWriter
{
    /// <inheritdoc/>
    public async Task<string?> GetOrNullAsync(
        string featureName,
        string? tenantId,
        CancellationToken ct = default)
    {
        Guid? tenantGuid = ParseTenantId(tenantId);
        await using GranitFeaturesDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        TenantFeatureOverride? row = await context.FeatureOverrides
            .AsNoTracking()
            .FirstOrDefaultAsync(
                o => o.TenantId == tenantGuid && o.FeatureName == featureName,
                ct).ConfigureAwait(false);

        return row?.Value;
    }

    /// <inheritdoc/>
    public async Task SetAsync(
        string featureName,
        string? tenantId,
        string value,
        CancellationToken ct = default)
    {
        Guid? tenantGuid = ParseTenantId(tenantId);
        await using GranitFeaturesDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        TenantFeatureOverride? existing = await context.FeatureOverrides
            .FirstOrDefaultAsync(
                o => o.TenantId == tenantGuid && o.FeatureName == featureName,
                ct).ConfigureAwait(false);

        if (existing is null)
        {
            context.FeatureOverrides.Add(new TenantFeatureOverride
            {
                TenantId = tenantGuid,
                FeatureName = featureName,
                Value = value,
            });
        }
        else
        {
            existing.Value = value;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(
        string featureName,
        string? tenantId,
        CancellationToken ct = default)
    {
        Guid? tenantGuid = ParseTenantId(tenantId);
        await using GranitFeaturesDbContext context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        TenantFeatureOverride? existing = await context.FeatureOverrides
            .FirstOrDefaultAsync(
                o => o.TenantId == tenantGuid && o.FeatureName == featureName,
                ct).ConfigureAwait(false);

        if (existing is null)
        {
            return;
        }

        context.FeatureOverrides.Remove(existing);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static Guid? ParseTenantId(string? tenantId) =>
        tenantId is not null && Guid.TryParse(tenantId, out Guid parsed) ? parsed : null;
}
