using Granit.Timeline.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Timeline.EntityFrameworkCore;

/// <summary>
/// EF Core DbContext for timeline persistence.
/// </summary>
/// <remarks>
/// Isolated from the host application's DbContext to avoid coupling.
/// Compatible with PostgreSQL (OVHcloud FR — European sovereignty, HDS compliant).
/// </remarks>
public sealed class TimelineDbContext : DbContext
{
    /// <summary>Activity stream entries (comments, system logs, internal notes).</summary>
    public DbSet<TimelineEntry> TimelineEntries => Set<TimelineEntry>();

    /// <summary>Attachment references linking entries to BlobStorage blobs.</summary>
    public DbSet<TimelineAttachment> TimelineAttachments => Set<TimelineAttachment>();

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelineDbContext"/> class.
    /// </summary>
    public TimelineDbContext(DbContextOptions<TimelineDbContext> options) : base(options) { }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TimelineDbContext).Assembly);
    }
}
