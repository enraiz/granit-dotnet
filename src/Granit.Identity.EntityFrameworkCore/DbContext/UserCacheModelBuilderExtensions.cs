using Granit.Identity.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Granit.Identity.EntityFrameworkCore.DbContext;

/// <summary>EF Core model builder extensions for the identity user cache schema.</summary>
public static class UserCacheModelBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="UserCacheEntry"/> entity: table name, column constraints,
    /// and indexes for efficient lookup and search.
    /// Call this from <c>OnModelCreating</c> in the host application's DbContext.
    /// </summary>
    public static ModelBuilder ConfigureIdentityUserCache(this ModelBuilder builder)
    {
        builder.Entity<UserCacheEntry>(entity =>
        {
            entity.ToTable("identity_user_cache_entries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ExternalUserId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(256);
            entity.Property(e => e.Email).HasMaxLength(512);
            entity.Property(e => e.FirstName).HasMaxLength(256);
            entity.Property(e => e.LastName).HasMaxLength(256);

            // Unique: one cache entry per user per tenant
            entity.HasIndex(e => new { e.TenantId, e.ExternalUserId })
                  .IsUnique()
                  .HasDatabaseName("uq_identity_user_cache_tenant_external_id");

            // Search indexes
            entity.HasIndex(e => new { e.TenantId, e.Username })
                  .HasDatabaseName("ix_identity_user_cache_tenant_username");

            entity.HasIndex(e => new { e.TenantId, e.Email })
                  .HasDatabaseName("ix_identity_user_cache_tenant_email");

            entity.HasIndex(e => new { e.TenantId, e.LastName, e.FirstName })
                  .HasDatabaseName("ix_identity_user_cache_tenant_name");
        });

        return builder;
    }
}
