using DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.DbContext;

/// <summary>
/// Implement this interface on the host application's <see cref="Microsoft.EntityFrameworkCore.DbContext"/>
/// to enable Foundation.Authorization.EntityFrameworkCore persistence.
/// Call <see cref="PermissionGrantModelBuilderExtensions.ConfigurePermissionGrants"/> in <c>OnModelCreating</c>.
/// </summary>
public interface IPermissionGrantDbContext
{
    /// <summary>Permission grants table. Configured with a unique index on (TenantId, Name, RoleName).</summary>
    DbSet<PermissionGrant> PermissionGrants { get; }
}
