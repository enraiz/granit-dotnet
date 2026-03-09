using Granit.Core.DataFiltering;
using Granit.Core.MultiTenancy;
using Granit.Persistence.Extensions;
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
    private readonly ICurrentTenant? _currentTenant;
    private readonly IDataFilter? _dataFilter;

    /// <summary>Activity stream entries (comments, system logs, internal notes).</summary>
    public DbSet<TimelineEntry> TimelineEntries => Set<TimelineEntry>();

    /// <summary>Attachment references linking entries to BlobStorage blobs.</summary>
    public DbSet<TimelineAttachment> TimelineAttachments => Set<TimelineAttachment>();

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelineDbContext"/> class.
    /// </summary>
    public TimelineDbContext(
        DbContextOptions<TimelineDbContext> options,
        ICurrentTenant? currentTenant = null,
        IDataFilter? dataFilter = null) : base(options)
    {
        _currentTenant = currentTenant;
        _dataFilter = dataFilter;
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TimelineDbContext).Assembly);
        modelBuilder.ApplyGranitConventions(_currentTenant, _dataFilter);
    }
}
