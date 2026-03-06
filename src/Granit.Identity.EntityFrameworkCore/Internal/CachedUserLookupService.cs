using Granit.Core.MultiTenancy;
using Granit.Identity.EntityFrameworkCore.Entities;
using Granit.Identity.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.Identity.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core–backed implementation of <see cref="IUserLookupService"/> with cache-aside strategy.
/// Replaces <c>NullUserLookupService</c> when <c>Granit.Identity.EntityFrameworkCore</c> is registered.
/// </summary>
internal sealed partial class CachedUserLookupService(
    IUserCacheStore store,
    IIdentityProvider identityProvider,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider,
    IOptions<UserCacheOptions> options,
    ILogger<CachedUserLookupService> logger) : IUserLookupService
{
    private readonly UserCacheOptions _options = options.Value;

    // -- Read (cache-aside) --

    public async Task<IdentityUser?> FindByIdAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        // Tenant-scoped or host-context lookup
        UserCacheEntry? entry = currentTenant.IsAvailable
            ? await store.FindByExternalIdAsync(userId, currentTenant.Id, cancellationToken).ConfigureAwait(false)
            : await store.FindFirstByExternalIdAsync(userId, cancellationToken).ConfigureAwait(false);

        if (entry is not null && IsFresh(entry))
        {
            return ToIdentityUser(entry);
        }

        // Cache miss or stale — fetch from identity provider
        try
        {
            IdentityUser? providerUser = await identityProvider.GetUserAsync(userId, cancellationToken)
                .ConfigureAwait(false);

            if (providerUser is not null)
            {
                var cacheEntry = ToCacheEntry(providerUser);
                await store.UpsertAsync(cacheEntry, cancellationToken).ConfigureAwait(false);
                return providerUser;
            }

            // Provider returned null — user doesn't exist in provider
            return entry is not null ? ToIdentityUser(entry) : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Graceful degradation: provider down, return stale data if available
            LogProviderError(ex, userId);
            return entry is not null ? ToIdentityUser(entry) : null;
        }
    }

    public async Task<IReadOnlyList<IdentityUser>> FindByIdsAsync(
        IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;
        IReadOnlyList<UserCacheEntry> cached = await store.FindByExternalIdsAsync(userIds, tenantId, cancellationToken)
            .ConfigureAwait(false);

        var result = new List<IdentityUser>(userIds.Count);
        var cachedDict = cached.ToDictionary(e => e.ExternalUserId);
        var toFetch = new List<string>();

        foreach (string id in userIds)
        {
            if (cachedDict.TryGetValue(id, out var entry) && IsFresh(entry))
            {
                result.Add(ToIdentityUser(entry));
            }
            else
            {
                toFetch.Add(id);
            }
        }

        if (toFetch.Count == 0)
        {
            return result;
        }

        // Fetch missing/stale from provider
        try
        {
            foreach (string id in toFetch)
            {
                IdentityUser? providerUser = await identityProvider.GetUserAsync(id, cancellationToken)
                    .ConfigureAwait(false);

                if (providerUser is not null)
                {
                    var cacheEntry = ToCacheEntry(providerUser);
                    await store.UpsertAsync(cacheEntry, cancellationToken).ConfigureAwait(false);
                    result.Add(providerUser);
                }
                else if (cachedDict.TryGetValue(id, out var staleEntry))
                {
                    result.Add(ToIdentityUser(staleEntry));
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogProviderBatchError(ex, toFetch.Count);
            // Return stale entries for remaining IDs
            foreach (string id in toFetch)
            {
                if (cachedDict.TryGetValue(id, out var staleEntry)
                    && !result.Any(u => u.Id == id))
                {
                    result.Add(ToIdentityUser(staleEntry));
                }
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<IdentityUser>> SearchAsync(
        string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;
        IReadOnlyList<UserCacheEntry> entries = await store.SearchAsync(searchTerm, tenantId, maxResults, cancellationToken)
            .ConfigureAwait(false);

        return entries.Select(ToIdentityUser).ToList();
    }

    // -- Sync --

    public async Task<IdentityUser?> RefreshByIdAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        IdentityUser? providerUser = await identityProvider.GetUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        if (providerUser is null)
        {
            return null;
        }

        var cacheEntry = ToCacheEntry(providerUser);
        await store.UpsertAsync(cacheEntry, cancellationToken).ConfigureAwait(false);
        return providerUser;
    }

    public async Task<int> RefreshAllAsync(CancellationToken cancellationToken = default)
    {
        int synced = 0;
        int offset = 0;
        const int pageSize = 100;

        while (true)
        {
            IReadOnlyList<IdentityUser> page = await identityProvider.GetUsersAsync(
                search: null, first: offset, max: pageSize, cancellationToken).ConfigureAwait(false);

            if (page.Count == 0)
            {
                break;
            }

            var entries = page.Select(ToCacheEntry).ToList();
            await store.UpsertManyAsync(entries, cancellationToken).ConfigureAwait(false);

            synced += page.Count;
            offset += page.Count;

            if (page.Count < pageSize)
            {
                break;
            }
        }

        LogRefreshAllCompleted(synced);
        return synced;
    }

    // -- RGPD --

    public async Task DeleteByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;
        await store.DeleteByExternalIdAsync(userId, tenantId, cancellationToken).ConfigureAwait(false);
        LogRgpdDelete(userId);
    }

    public async Task PseudonymizeByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;
        await store.PseudonymizeAsync(userId, tenantId, cancellationToken).ConfigureAwait(false);
        LogRgpdPseudonymize(userId);
    }

    // -- Helpers --

    private bool IsFresh(UserCacheEntry entry) =>
        timeProvider.GetUtcNow() - entry.LastSyncedAt < _options.StalenessThreshold;

    private UserCacheEntry ToCacheEntry(IdentityUser user) => new()
    {
        ExternalUserId = user.Id,
        Username = user.Username,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Enabled = user.Enabled,
        LastSyncedAt = timeProvider.GetUtcNow(),
        TenantId = currentTenant.IsAvailable ? currentTenant.Id : null
    };

    private static IdentityUser ToIdentityUser(UserCacheEntry entry) => new(
        Id: entry.ExternalUserId,
        Username: entry.Username,
        Email: entry.Email,
        FirstName: entry.FirstName,
        LastName: entry.LastName,
        Enabled: entry.Enabled);

    // -- Source-generated log messages --

    [LoggerMessage(Level = LogLevel.Warning, Message = "Identity provider error while fetching user {UserId}, returning stale cache")]
    private partial void LogProviderError(Exception exception, string userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Identity provider error during batch fetch of {Count} users, returning stale cache")]
    private partial void LogProviderBatchError(Exception exception, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "[AUDIT] Full user cache refresh completed: {Count} users synchronized")]
    private partial void LogRefreshAllCompleted(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "[AUDIT] RGPD erasure: user cache entry deleted for user {UserId}")]
    private partial void LogRgpdDelete(string userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[AUDIT] RGPD pseudonymization: user cache entry anonymized for user {UserId}")]
    private partial void LogRgpdPseudonymize(string userId);
}
