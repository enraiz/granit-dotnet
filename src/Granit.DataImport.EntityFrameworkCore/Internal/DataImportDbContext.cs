using Granit.DataImport.Domain;
using Granit.DataImport.EntityFrameworkCore.Internal.Configurations;
using Granit.DataImport.EntityFrameworkCore.Internal.Entities;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataImport.EntityFrameworkCore.Internal;

/// <summary>
/// Isolated DbContext for the DataImport persistence layer.
/// Owns <see cref="ImportJob"/>, <see cref="SavedMappingEntity"/>, and <see cref="ExternalIdMappingEntity"/>.
/// </summary>
internal sealed class DataImportDbContext(DbContextOptions<DataImportDbContext> options)
    : DbContext(options)
{
    public DbSet<ImportJob> ImportJobs { get; set; } = null!;
    public DbSet<SavedMappingEntity> SavedMappings { get; set; } = null!;
    public DbSet<ExternalIdMappingEntity> ExternalIdMappings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ImportJobConfiguration());
        modelBuilder.ApplyConfiguration(new SavedMappingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ExternalIdMappingEntityConfiguration());
    }
}
