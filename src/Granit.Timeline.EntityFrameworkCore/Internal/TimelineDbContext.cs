using Granit.Core.DataFiltering;
using Granit.Core.MultiTenancy;
using Granit.Persistence.Extensions;
using Granit.Timeline.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Timeline.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core DbContext for timeline persistence.
/// </summary>
/// <remarks>
/// Isolated from the host application's DbContext to avoid coupling.
/// Compatible with PostgreSQL (OVHcloud FR — European sovereignty, ISO 27001 compliant).
/// </remarks>
internal sealed class TimelineDbContext(
    DbContextOptions<TimelineDbContext> options,
    ICurrentTenant? currentTenant = null,
    IDataFilter? dataFilter = null)
    : DbContext(options)
{
    /// <summary>Activity stream entries (comments, system logs, internal notes).</summary>
    public DbSet<TimelineEntry> TimelineEntries => Set<TimelineEntry>();

    /// <summary>Attachment references linking entries to BlobStorage blobs.</summary>
    public DbSet<TimelineAttachment> TimelineAttachments => Set<TimelineAttachment>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TimelineDbContext).Assembly);
        modelBuilder.ApplyGranitConventions(currentTenant, dataFilter);
    }
}
