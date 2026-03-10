using Granit.Core.DataFiltering;
using Granit.Core.MultiTenancy;
using Granit.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Granit.BackgroundJobs.EntityFrameworkCore.Internal;

/// <summary>
/// Dedicated EF Core DbContext for Granit background job administrative state.
/// </summary>
/// <remarks>
/// <para>
/// Isolated from the host application's DbContext to avoid coupling. Stores only
/// the administrative record (<see cref="BackgroundJobDefinition"/>) — Wolverine messages
/// and the Outbox live in the Wolverine transport schema.
/// </para>
/// <para>
/// Compatible with SQL Server and PostgreSQL.
/// </para>
/// </remarks>
internal sealed class BackgroundJobsDbContext : DbContext
{
    private readonly ICurrentTenant? _currentTenant;
    private readonly IDataFilter? _dataFilter;

    /// <summary>
    /// Initializes a new instance of <see cref="BackgroundJobsDbContext"/>.
    /// </summary>
    public BackgroundJobsDbContext(
        DbContextOptions<BackgroundJobsDbContext> options,
        ICurrentTenant? currentTenant = null,
        IDataFilter? dataFilter = null)
        : base(options)
    {
        _currentTenant = currentTenant;
        _dataFilter = dataFilter;
    }

    /// <summary>Administrative records for all registered recurring jobs.</summary>
    public DbSet<BackgroundJobDefinition> Jobs { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new BackgroundJobDefinitionConfiguration());
        modelBuilder.ApplyGranitConventions(_currentTenant, _dataFilter);
    }
}
