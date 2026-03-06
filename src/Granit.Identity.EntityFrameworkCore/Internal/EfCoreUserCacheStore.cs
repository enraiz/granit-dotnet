using Granit.Identity.EntityFrameworkCore.DbContext;
using Granit.Identity.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Granit.Identity.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="IUserCacheStore"/>.
/// All read operations use <c>AsNoTracking</c> for performance.
/// </summary>
internal sealed class EfCoreUserCacheStore<TContext>(TContext context)
    : IUserCacheStore
    where TContext : Microsoft.EntityFrameworkCore.DbContext, IUserCacheDbContext
{
    // -- Read --

    public Task<UserCacheEntry?> FindByExternalIdAsync(
        string externalUserId, Guid? tenantId, CancellationToken cancellationToken = default) =>
        context.UserCacheEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.TenantId == tenantId && e.ExternalUserId == externalUserId,
                cancellationToken);

    public Task<UserCacheEntry?> FindFirstByExternalIdAsync(
        string externalUserId, CancellationToken cancellationToken = default) =>
        context.UserCacheEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.ExternalUserId == externalUserId,
                cancellationToken);

    public async Task<IReadOnlyList<UserCacheEntry>> FindByExternalIdsAsync(
        IReadOnlyCollection<string> externalUserIds, Guid? tenantId, CancellationToken cancellationToken = default) =>
        await context.UserCacheEntries
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && externalUserIds.Contains(e.ExternalUserId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<UserCacheEntry>> SearchAsync(
        string term, Guid? tenantId, int maxResults, CancellationToken cancellationToken = default)
    {
        string pattern = $"%{term}%";

        return await context.UserCacheEntries
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId
                && (EF.Functions.Like(e.Username ?? "", pattern)
                    || EF.Functions.Like(e.Email ?? "", pattern)
                    || EF.Functions.Like(e.FirstName ?? "", pattern)
                    || EF.Functions.Like(e.LastName ?? "", pattern)))
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Take(maxResults)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    // -- Diagnostics --

    public Task<int> GetCountAsync(Guid? tenantId, CancellationToken cancellationToken = default) =>
        context.UserCacheEntries
            .AsNoTracking()
            .CountAsync(e => e.TenantId == tenantId, cancellationToken);

    public Task<int> GetStaleCountAsync(
        Guid? tenantId, DateTimeOffset threshold, CancellationToken cancellationToken = default) =>
        context.UserCacheEntries
            .AsNoTracking()
            .CountAsync(e => e.TenantId == tenantId && e.LastSyncedAt < threshold, cancellationToken);

    public async Task<(DateTimeOffset? Oldest, DateTimeOffset? Newest)> GetSyncRangeAsync(
        Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var query = context.UserCacheEntries
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId);

        if (!await query.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return (null, null);
        }

        var oldest = await query.MinAsync(e => e.LastSyncedAt, cancellationToken).ConfigureAwait(false);
        var newest = await query.MaxAsync(e => e.LastSyncedAt, cancellationToken).ConfigureAwait(false);

        return (oldest, newest);
    }

    // -- Write --

    public async Task UpsertAsync(UserCacheEntry entry, CancellationToken cancellationToken = default)
    {
        UserCacheEntry? existing = await context.UserCacheEntries
            .FirstOrDefaultAsync(
                e => e.TenantId == entry.TenantId && e.ExternalUserId == entry.ExternalUserId,
                cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            context.UserCacheEntries.Add(entry);
        }
        else
        {
            existing.Username = entry.Username;
            existing.Email = entry.Email;
            existing.FirstName = entry.FirstName;
            existing.LastName = entry.LastName;
            existing.Enabled = entry.Enabled;
            existing.LastSyncedAt = entry.LastSyncedAt;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertManyAsync(
        IReadOnlyList<UserCacheEntry> entries, CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var externalIds = entries.Select(e => e.ExternalUserId).ToHashSet();
        Guid? tenantId = entries[0].TenantId;

        var existingEntries = await context.UserCacheEntries
            .Where(e => e.TenantId == tenantId && externalIds.Contains(e.ExternalUserId))
            .ToDictionaryAsync(e => e.ExternalUserId, cancellationToken).ConfigureAwait(false);

        foreach (var entry in entries)
        {
            if (existingEntries.TryGetValue(entry.ExternalUserId, out var existing))
            {
                existing.Username = entry.Username;
                existing.Email = entry.Email;
                existing.FirstName = entry.FirstName;
                existing.LastName = entry.LastName;
                existing.Enabled = entry.Enabled;
                existing.LastSyncedAt = entry.LastSyncedAt;
            }
            else
            {
                context.UserCacheEntries.Add(entry);
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // -- RGPD --

    public async Task DeleteByExternalIdAsync(
        string externalUserId, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        await context.UserCacheEntries
            .Where(e => e.TenantId == tenantId && e.ExternalUserId == externalUserId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAllByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        await context.UserCacheEntries
            .Where(e => e.TenantId == tenantId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task PseudonymizeAsync(
        string externalUserId, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        await context.UserCacheEntries
            .Where(e => e.TenantId == tenantId && e.ExternalUserId == externalUserId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Username, "anonymized")
                .SetProperty(e => e.Email, "anonymized@anonymized.local")
                .SetProperty(e => e.FirstName, "Anonymized")
                .SetProperty(e => e.LastName, "User")
                .SetProperty(e => e.Enabled, false),
                cancellationToken).ConfigureAwait(false);
    }
}
