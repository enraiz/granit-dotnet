using Granit.Querying.SavedViews;
using Microsoft.EntityFrameworkCore;

namespace Granit.Querying.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="ISavedViewStore"/>.
/// Performs CRUD operations on <see cref="SavedView"/> via <see cref="QueryingDbContext"/>.
/// </summary>
internal sealed class EfCoreSavedViewStore(
    IDbContextFactory<QueryingDbContext> contextFactory) : ISavedViewStore
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<SavedView>> GetListAsync(
        string entityType, string userId, Guid? tenantId, CancellationToken ct = default)
    {
        await using QueryingDbContext context = await contextFactory
            .CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.SavedViews
            .AsNoTracking()
            .Where(v => v.EntityType == entityType
                && v.TenantId == tenantId
                && (v.UserId == userId || v.IsShared))
            .OrderBy(v => v.Name)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SavedView?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using QueryingDbContext context = await contextFactory
            .CreateDbContextAsync(ct).ConfigureAwait(false);

        return await context.SavedViews
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CreateAsync(SavedView view, CancellationToken ct = default)
    {
        await using QueryingDbContext context = await contextFactory
            .CreateDbContextAsync(ct).ConfigureAwait(false);

        context.SavedViews.Add(view);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(SavedView view, CancellationToken ct = default)
    {
        await using QueryingDbContext context = await contextFactory
            .CreateDbContextAsync(ct).ConfigureAwait(false);

        context.SavedViews.Update(view);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using QueryingDbContext context = await contextFactory
            .CreateDbContextAsync(ct).ConfigureAwait(false);

        SavedView? view = await context.SavedViews
            .FirstOrDefaultAsync(v => v.Id == id, ct).ConfigureAwait(false);

        if (view is not null)
        {
            context.SavedViews.Remove(view);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task SetDefaultAsync(
        Guid id, string userId, string entityType, CancellationToken ct = default)
    {
        await using QueryingDbContext context = await contextFactory
            .CreateDbContextAsync(ct).ConfigureAwait(false);

        // Unset any previous default for the same user and entity type
        List<SavedView> previousDefaults = await context.SavedViews
            .Where(v => v.EntityType == entityType
                && v.UserId == userId
                && v.IsDefault)
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (SavedView previous in previousDefaults)
        {
            previous.IsDefault = false;
        }

        // Set the new default
        SavedView? target = await context.SavedViews
            .FirstOrDefaultAsync(v => v.Id == id, ct).ConfigureAwait(false);

        if (target is not null)
        {
            target.IsDefault = true;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
