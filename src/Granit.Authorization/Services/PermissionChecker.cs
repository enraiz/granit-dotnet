using Granit.Authorization.Abstractions;
using Granit.Authorization.Cache;
using Granit.Authorization.Options;
using Granit.Caching;
using Granit.Core.MultiTenancy;
using Granit.Security;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Granit.Authorization.Services;

/// <summary>
/// Scoped permission checker implementing the full RBAC verification pipeline:
/// <list type="number">
/// <item>AlwaysAllow (dev/test) → granted</item>
/// <item>Not authenticated → denied</item>
/// <item>AdminRole bypass (root of trust) → granted without DB</item>
/// <item>Permission undefined → <see cref="InvalidOperationException"/></item>
/// <item>For each role: cache hit or store query; any true → granted</item>
/// </list>
/// Cache key format: <c>perm:{tenantId|"global"}:{roleName}:{permissionName}</c>
/// </summary>
internal sealed class PermissionChecker(
    ICurrentUserService currentUserService,
    ICurrentTenant currentTenant,
    IPermissionDefinitionManager definitionManager,
    IPermissionGrantStore grantStore,
    ICacheService<PermissionGrantCacheItem> cache,
    IOptions<GranitAuthorizationOptions> options) : IPermissionChecker
{
    /// <inheritdoc />
    public async Task<bool> IsGrantedAsync(string permissionName, CancellationToken cancellationToken = default)
    {
        GranitAuthorizationOptions opts = options.Value;

        if (opts.AlwaysAllow)
        {
            return true;
        }

        if (!currentUserService.IsAuthenticated)
        {
            return false;
        }

        if (opts.AdminRoles.Any(currentUserService.IsInRole))
        {
            return true;
        }

        if (!definitionManager.Exists(permissionName))
        {
            throw new InvalidOperationException(
                $"Permission '{permissionName}' is not defined. Register it via IPermissionDefinitionProvider.");
        }

        Guid? tenantId = currentTenant.Id;
        IReadOnlyList<string> roles = currentUserService.GetRoles();

        foreach (string role in roles)
        {
            PermissionGrantCacheItem result = await cache.GetOrAddAsync(
                BuildCacheKey(tenantId, role, permissionName),
                async ct => new PermissionGrantCacheItem
                {
                    IsGranted = await grantStore.IsGrantedAsync(role, permissionName, tenantId, ct)
                },
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = opts.CacheDuration
                },
                cancellationToken);

            if (result.IsGranted)
            {
                return true;
            }
        }

        return false;
    }

    internal static string BuildCacheKey(Guid? tenantId, string roleName, string permissionName) =>
        $"perm:{tenantId?.ToString() ?? "global"}:{roleName}:{permissionName}";
}
