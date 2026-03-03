using Granit.Querying.SavedViews;
using Microsoft.EntityFrameworkCore;

namespace Granit.Querying.EntityFrameworkCore.Internal;

/// <summary>
/// Isolated DbContext for the Querying persistence layer.
/// Owns <see cref="SavedView"/>.
/// </summary>
internal sealed class QueryingDbContext(DbContextOptions<QueryingDbContext> options)
    : DbContext(options)
{
    public DbSet<SavedView> SavedViews { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new SavedViewEntityConfiguration());
    }
}
