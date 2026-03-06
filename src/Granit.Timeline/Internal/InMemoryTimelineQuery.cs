using Granit.Timeline.Abstractions;
using Granit.Timeline.Domain;

namespace Granit.Timeline.Internal;

/// <summary>
/// In-memory implementation of <see cref="ITimelineQuery"/> for development and tests.
/// </summary>
internal sealed class InMemoryTimelineQuery(InMemoryTimelineStore store) : ITimelineQuery
{
    /// <inheritdoc/>
    public Task<TimelineStreamPage> GetStreamAsync(
        string entityType,
        string entityId,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default)
    {
        var entries = store.Entries.Values
            .Where(e => e.EntityType == entityType
                        && e.EntityId == entityId
                        && !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .ToList();

        int totalCount = entries.Count;

        var page = entries
            .Skip(skip)
            .Take(take)
            .Select(MapToStreamEntry)
            .ToList();

        return Task.FromResult(new TimelineStreamPage
        {
            Items = page,
            TotalCount = totalCount,
        });
    }

    private TimelineStreamEntry MapToStreamEntry(TimelineEntry entry) =>
        new()
        {
            Id = entry.Id,
            OccurredAt = entry.CreatedAt,
            EntryType = entry.EntryType switch
            {
                TimelineEntryType.Comment => TimelineStreamEntryType.Comment,
                TimelineEntryType.InternalNote => TimelineStreamEntryType.InternalNote,
                TimelineEntryType.SystemLog => TimelineStreamEntryType.SystemLog,
                _ => TimelineStreamEntryType.SystemLog,
            },
            AuthorId = entry.AuthorId,
            AuthorName = entry.AuthorName,
            Body = entry.Body,
            ParentEntryId = entry.ParentEntryId,
            Attachments = store.Attachments.Values
                .Where(a => a.EntryId == entry.Id)
                .Select(a => new TimelineAttachmentInfo(a.Id, a.BlobId, a.FileName, a.ContentType, a.SizeBytes))
                .ToList(),
        };
}
