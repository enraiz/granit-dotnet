using Granit.Core.MultiTenancy;
using Granit.Guids;
using Granit.Security;
using Granit.Timeline.Abstractions;
using Granit.Timeline.Domain;
using Granit.Timing;
using Microsoft.EntityFrameworkCore;

namespace Granit.Timeline.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of <see cref="ITimelineStore"/> backed by PostgreSQL.
/// </summary>
/// <remarks>
/// Each operation creates and disposes its own <see cref="TimelineDbContext"/> via
/// <see cref="IDbContextFactory{TContext}"/>, making it safe for concurrent request handling.
/// </remarks>
internal sealed class EfCoreTimelineStore(
    IDbContextFactory<TimelineDbContext> dbContextFactory,
    IClock clock,
    ICurrentUserService currentUser,
    IGuidGenerator guidGenerator,
    ICurrentTenant currentTenant) : ITimelineStore
{
    /// <inheritdoc/>
    public async Task<TimelineEntry> PostEntryAsync(
        string entityType,
        string entityId,
        TimelineEntryType entryType,
        string body,
        Guid? parentEntryId = null,
        CancellationToken ct = default)
    {
        TimelineEntry entry = new()
        {
            Id = guidGenerator.Create(),
            EntityType = entityType,
            EntityId = entityId,
            EntryType = entryType,
            Body = body,
            AuthorId = currentUser.UserId ?? string.Empty,
            AuthorName = currentUser.UserName ?? string.Empty,
            ParentEntryId = parentEntryId,
            CreatedAt = clock.Now,
            CreatedBy = currentUser.UserId ?? string.Empty,
            TenantId = currentTenant.IsAvailable ? currentTenant.Id : null,
        };

        await using TimelineDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        db.TimelineEntries.Add(entry);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return entry;
    }

    /// <inheritdoc/>
    public async Task DeleteEntryAsync(Guid entryId, CancellationToken ct = default)
    {
        await using TimelineDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        TimelineEntry entry = await db.TimelineEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entryId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Timeline entry '{entryId}' not found.");

        if (entry.EntryType == TimelineEntryType.SystemLog)
        {
            throw new InvalidOperationException("System log entries are immutable and cannot be deleted (HDS audit trail).");
        }

        entry.IsDeleted = true;
        entry.DeletedAt = clock.Now;
        entry.DeletedBy = currentUser.UserId;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<TimelineAttachment> AddAttachmentAsync(
        Guid entryId,
        Guid blobId,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken ct = default)
    {
        await using TimelineDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        bool entryExists = await db.TimelineEntries
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Id == entryId, ct).ConfigureAwait(false);

        if (!entryExists)
        {
            throw new KeyNotFoundException($"Timeline entry '{entryId}' not found.");
        }

        TimelineAttachment attachment = new()
        {
            Id = guidGenerator.Create(),
            EntryId = entryId,
            BlobId = blobId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            CreatedAt = clock.Now,
            CreatedBy = currentUser.UserId ?? string.Empty,
            TenantId = currentTenant.IsAvailable ? currentTenant.Id : null,
        };

        db.TimelineAttachments.Add(attachment);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return attachment;
    }
}
