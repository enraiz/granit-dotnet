using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Granit.Authentication.ApiKeys.EntityFrameworkCore.EntityConfigurations;

/// <summary>
/// EF Core configuration for <see cref="ApiKeyEntry"/>.
/// </summary>
internal sealed class ApiKeyEntryConfiguration : IEntityTypeConfiguration<ApiKeyEntry>
{
    public void Configure(EntityTypeBuilder<ApiKeyEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ApiKeys");

        builder.HasKey(e => e.Id);

        // Unique index on HashedKey for O(1) lookups
        builder.HasIndex(e => e.HashedKey)
            .IsUnique()
            .HasDatabaseName("IX_ApiKeys_HashedKey");

        // Index on TenantId for multi-tenant queries
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_ApiKeys_TenantId");

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.HashedKey).HasMaxLength(64).IsRequired(); // SHA-256 hex = 64 chars
        builder.Property(e => e.Prefix).HasMaxLength(20).IsRequired();
        builder.Property(e => e.LastFourChars).HasMaxLength(4).IsRequired();
        builder.Property(e => e.Environment).HasMaxLength(10).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(200);
        builder.Property(e => e.ModifiedBy).HasMaxLength(200);
        builder.Property(e => e.DeletedBy).HasMaxLength(200);

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.CacheBehavior).HasConversion<string>().HasMaxLength(20);

        // JSON columns for collections (PostgreSQL jsonb)
        builder.Property(e => e.Permissions)
            .HasColumnType("jsonb");

        builder.Property(e => e.AllowedCidrs)
            .HasColumnType("jsonb");

        // Soft delete query filter
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
