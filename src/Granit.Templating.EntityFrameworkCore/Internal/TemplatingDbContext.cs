using Microsoft.EntityFrameworkCore;

namespace Granit.Templating.EntityFrameworkCore.Internal;

/// <summary>
/// Dedicated EF Core DbContext for Granit template revisions.
/// </summary>
/// <remarks>
/// Isolated from the host application's DbContext to avoid coupling.
/// Compatible with PostgreSQL and SQL Server.
/// </remarks>
internal sealed class TemplatingDbContext(DbContextOptions<TemplatingDbContext> options)
    : DbContext(options)
{
    /// <summary>All template revisions (Draft, Published, Deprecated).</summary>
    public DbSet<TemplateRevisionEntity> TemplateRevisions { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new TemplateRevisionEntityConfiguration());
    }
}
