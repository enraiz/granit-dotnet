using DigitalDynamics.Foundation.Authorization.Abstractions;
using DigitalDynamics.Foundation.Authorization.Cache;
using DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.DbContext;
using DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.Entities;
using DigitalDynamics.Foundation.Authorization.Services;
using DigitalDynamics.Foundation.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.Services;

/// <summary>
/// EF Core implementation of <see cref="IPermissionManager"/>.
/// Provides grant management with mandatory HDS audit logging on every mutation.
/// Cache is invalidated after each <see cref="SetAsync"/> to maintain consistency.
/// </summary>
internal sealed class PermissionManager<TContext>(
    TContext context,
    IPermissionDefinitionManager definitionManager,
    ICacheService<PermissionGrantCacheItem> cache,
    ILogger<PermissionManager<TContext>> logger)
    : IPermissionManager
    where TContext : Microsoft.EntityFrameworkCore.DbContext, IPermissionGrantDbContext
{
    /// <inheritdoc />
    public async Task SetAsync(
        string permissionName,
        string roleName,
        Guid? tenantId,
        bool isGranted,
        CancellationToken cancellationToken = default)
    {
        if (!definitionManager.Exists(permissionName))
        {
            throw new InvalidOperationException(
                $"Permission '{permissionName}' is not defined. Register it via IPermissionDefinitionProvider.");
        }

        PermissionGrant? existing = await context.PermissionGrants
            .FirstOrDefaultAsync(
                g => g.TenantId == tenantId && g.Name == permissionName && g.RoleName == roleName,
                cancellationToken);

        if (isGranted && existing is null)
        {
            context.PermissionGrants.Add(new PermissionGrant
            {
                Id = Guid.NewGuid(), // overridden by AuditedEntityInterceptor in production
                Name = permissionName,
                RoleName = roleName,
                TenantId = tenantId
            });
        }
        else if (!isGranted && existing is not null)
        {
            context.PermissionGrants.Remove(existing);
        }
        else
        {
            return; // no-op: state already matches requested value
        }

        await context.SaveChangesAsync(cancellationToken);

        // Cache invalidation — same key format as PermissionChecker.BuildCacheKey
        await cache.RemoveAsync(
            PermissionChecker.BuildCacheKey(tenantId, roleName, permissionName),
            cancellationToken);

        // HDS audit trail: emitted as structured log → Serilog → OTLP → Loki (3-year retention)
        // RGPD: no personal data — only role name, permission name, tenant scope
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[AUDIT] Permission {Change}: permission={PermissionName} role={RoleName} tenantId={TenantId}",
                isGranted ? "Granted" : "Revoked",
                permissionName,
                roleName,
                tenantId);
        }
    }

    /// <inheritdoc />
    public Task<bool> IsGrantedAsync(
        string permissionName,
        string roleName,
        Guid? tenantId,
        CancellationToken cancellationToken = default) =>
        context.PermissionGrants
            .AsNoTracking()
            .AnyAsync(
                g => g.TenantId == tenantId && g.Name == permissionName && g.RoleName == roleName,
                cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetGrantedPermissionsAsync(
        string roleName,
        Guid? tenantId,
        CancellationToken cancellationToken = default) =>
        await context.PermissionGrants
            .AsNoTracking()
            .Where(g => g.TenantId == tenantId && g.RoleName == roleName)
            .Select(g => g.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetGrantedRolesAsync(
        string permissionName,
        Guid? tenantId,
        CancellationToken cancellationToken = default) =>
        await context.PermissionGrants
            .AsNoTracking()
            .Where(g => g.TenantId == tenantId && g.Name == permissionName)
            .Select(g => g.RoleName)
            .ToListAsync(cancellationToken);
}
