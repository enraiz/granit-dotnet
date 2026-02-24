using Granit.Authorization.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Granit.Authorization.EntityFrameworkCore.DbContext;

/// <summary>EF Core model builder extensions for the permission grants schema.</summary>
public static class PermissionGrantModelBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="PermissionGrant"/> entity: table name, column constraints,
    /// and unique composite index on (TenantId, Name, RoleName).
    /// Call this from <c>OnModelCreating</c> in the host application's DbContext.
    /// </summary>
    public static ModelBuilder ConfigurePermissionGrants(this ModelBuilder builder)
    {
        builder.Entity<PermissionGrant>(entity =>
        {
            entity.ToTable("permission_grants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.RoleName).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Name, e.RoleName })
                  .IsUnique()
                  .HasDatabaseName("uq_permission_grants_tenant_name_role");
        });

        return builder;
    }
}
