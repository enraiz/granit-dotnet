using Granit.DataImport.EntityFrameworkCore.Internal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Granit.DataImport.EntityFrameworkCore.Internal.Configurations;

/// <summary>
/// EF Core configuration for <see cref="SavedMappingEntity"/>.
/// </summary>
internal sealed class SavedMappingEntityConfiguration : IEntityTypeConfiguration<SavedMappingEntity>
{
    public void Configure(EntityTypeBuilder<SavedMappingEntity> builder)
    {
        builder.ToTable("data_import_saved_mappings");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DefinitionName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TenantId);
        builder.Property(e => e.MappingsJson).IsRequired();
        builder.Property(e => e.SavedAt).IsRequired();
        builder.Property(e => e.SavedBy).HasMaxLength(200).IsRequired();

        builder.HasIndex(e => new { e.DefinitionName, e.TenantId })
            .IsUnique()
            .HasDatabaseName("ix_saved_mappings_def_tenant");
    }
}
