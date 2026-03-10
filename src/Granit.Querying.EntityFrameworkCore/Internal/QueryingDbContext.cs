using Granit.Core.DataFiltering;
using Granit.Core.MultiTenancy;
using Granit.Persistence.Extensions;
using Granit.Querying.SavedViews;
using Microsoft.EntityFrameworkCore;

namespace Granit.Querying.EntityFrameworkCore.Internal;

/// <summary>
/// Isolated DbContext for the Querying persistence layer.
/// Owns <see cref="SavedView"/>.
/// </summary>
internal sealed class QueryingDbContext : DbContext
{
    private readonly ICurrentTenant? _currentTenant;
    private readonly IDataFilter? _dataFilter;

    /// <summary>
    /// Initializes a new instance of <see cref="QueryingDbContext"/>.
    /// </summary>
    public QueryingDbContext(
        DbContextOptions<QueryingDbContext> options,
        ICurrentTenant? currentTenant = null,
        IDataFilter? dataFilter = null)
        : base(options)
    {
        _currentTenant = currentTenant;
        _dataFilter = dataFilter;
    }

    public DbSet<SavedView> SavedViews { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new SavedViewEntityConfiguration());
        modelBuilder.ApplyGranitConventions(_currentTenant, _dataFilter);
    }
}
