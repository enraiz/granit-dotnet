using Granit.DataImport.Domain;
using Granit.DataImport.EntityFrameworkCore.Internal.Configurations;
using Granit.DataImport.EntityFrameworkCore.Internal.Entities;
using Granit.DataImport.Export;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataImport.EntityFrameworkCore.Internal;

/// <summary>
/// Isolated DbContext for the DataImport persistence layer.
/// Owns <see cref="ImportJob"/>, <see cref="SavedMappingEntity"/>, <see cref="ExternalIdMappingEntity"/>,
/// <see cref="ExportJob"/>, and <see cref="ExportPresetEntity"/>.
/// </summary>
internal sealed class DataImportDbContext(DbContextOptions<DataImportDbContext> options)
    : DbContext(options)
{
    public DbSet<ImportJob> ImportJobs { get; set; } = null!;
    public DbSet<SavedMappingEntity> SavedMappings { get; set; } = null!;
    public DbSet<ExternalIdMappingEntity> ExternalIdMappings { get; set; } = null!;
    public DbSet<ExportJob> ExportJobs { get; set; } = null!;
    public DbSet<ExportPresetEntity> ExportPresets { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ImportJobConfiguration());
        modelBuilder.ApplyConfiguration(new SavedMappingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ExternalIdMappingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ExportJobConfiguration());
        modelBuilder.ApplyConfiguration(new ExportPresetEntityConfiguration());
    }
}
