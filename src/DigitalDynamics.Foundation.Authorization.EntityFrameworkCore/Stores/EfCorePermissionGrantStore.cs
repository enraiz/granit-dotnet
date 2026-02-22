using DigitalDynamics.Foundation.Authorization.Abstractions;
using DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.DbContext;
using Microsoft.EntityFrameworkCore;

namespace DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.Stores;

/// <summary>
/// EF Core implementation of <see cref="IPermissionGrantStore"/>.
/// Queries the <c>permission_grants</c> table with <c>AsNoTracking</c> for read-only performance.
/// </summary>
internal sealed class EfCorePermissionGrantStore<TContext>(TContext context)
    : IPermissionGrantStore
    where TContext : Microsoft.EntityFrameworkCore.DbContext, IPermissionGrantDbContext
{
    /// <inheritdoc />
    public Task<bool> IsGrantedAsync(
        string roleName,
        string permissionName,
        Guid? tenantId,
        CancellationToken cancellationToken = default) =>
        context.PermissionGrants
            .AsNoTracking()
            .AnyAsync(
                g => g.TenantId == tenantId && g.Name == permissionName && g.RoleName == roleName,
                cancellationToken);
}
