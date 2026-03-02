using Granit.Timeline.Abstractions;
using Granit.Timeline.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Timeline.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of <see cref="ITimelineQuery"/> backed by PostgreSQL.
/// </summary>
/// <remarks>
/// Queries use <c>AsNoTracking</c> for read performance. The soft-delete query filter
/// on <see cref="TimelineEntry"/> automatically excludes deleted entries.
/// </remarks>
internal sealed class EfCoreTimelineQuery(
    IDbContextFactory<TimelineDbContext> dbContextFactory) : ITimelineQuery
{
    /// <inheritdoc/>
    public async Task<TimelineStreamPage> GetStreamAsync(
        string entityType,
        string entityId,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default)
    {
        await using TimelineDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        IQueryable<TimelineEntry> query = db.TimelineEntries
            .AsNoTracking()
            .Where(e => e.EntityType == entityType && e.EntityId == entityId);

        int totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        List<TimelineEntry> entries = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct).ConfigureAwait(false);

        List<Guid> entryIds = entries.Select(e => e.Id).ToList();

        List<TimelineAttachment> attachments = await db.TimelineAttachments
            .AsNoTracking()
            .Where(a => entryIds.Contains(a.EntryId))
            .ToListAsync(ct).ConfigureAwait(false);

        ILookup<Guid, TimelineAttachment> attachmentLookup = attachments.ToLookup(a => a.EntryId);

        List<TimelineStreamEntry> items = entries
            .Select(e => new TimelineStreamEntry
            {
                Id = e.Id,
                OccurredAt = e.CreatedAt,
                EntryType = e.EntryType switch
                {
                    TimelineEntryType.Comment => TimelineStreamEntryType.Comment,
                    TimelineEntryType.InternalNote => TimelineStreamEntryType.InternalNote,
                    TimelineEntryType.SystemLog => TimelineStreamEntryType.SystemLog,
                    _ => TimelineStreamEntryType.SystemLog,
                },
                AuthorId = e.AuthorId,
                AuthorName = e.AuthorName,
                Body = e.Body,
                ParentEntryId = e.ParentEntryId,
                Attachments = attachmentLookup[e.Id]
                    .Select(a => new AttachmentInfo(a.Id, a.BlobId, a.FileName, a.ContentType, a.SizeBytes))
                    .ToList(),
            })
            .ToList();

        return new TimelineStreamPage
        {
            Items = items,
            TotalCount = totalCount,
        };
    }
}
