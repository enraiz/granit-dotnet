using Granit.DataExchange.EntityFrameworkCore.Internal.Export.Configurations;
using Granit.DataExchange.EntityFrameworkCore.Internal.Export.Entities;
using Granit.DataExchange.EntityFrameworkCore.Internal.Import.Configurations;
using Granit.DataExchange.EntityFrameworkCore.Internal.Import.Entities;
using Granit.DataExchange.Export;
using Granit.DataExchange.Import.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.DataExchange.EntityFrameworkCore.Internal;

/// <summary>
/// Isolated DbContext for the DataExchange persistence layer.
/// Owns <see cref="ImportJob"/>, <see cref="SavedMappingEntity"/>, <see cref="ExternalIdMappingEntity"/>,
/// <see cref="ExportJob"/>, and <see cref="ExportPresetEntity"/>.
/// </summary>
internal sealed class DataExchangeDbContext(DbContextOptions<DataExchangeDbContext> options)
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
