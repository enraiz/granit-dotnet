using Granit.Timeline.Abstractions;
using Granit.Timeline.Domain;
using Granit.Timeline.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.Timeline.Endpoints.Endpoints;

/// <summary>
/// POST/DELETE endpoints for creating and soft-deleting timeline entries.
/// </summary>
internal static class TimelineEntryEndpoints
{
    internal static RouteGroupBuilder MapEntryEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{entityType}/{entityId}/entries", PostEntryAsync)
            .WithName("PostTimelineEntry")
            .WithSummary("Posts a new comment, internal note, or system log entry.");

        group.MapDelete("/{entityType}/{entityId}/entries/{entryId:guid}", DeleteEntryAsync)
            .WithName("DeleteTimelineEntry")
            .WithSummary("Soft-deletes a comment or internal note (RGPD right to erasure).");

        return group;
    }

    private static async Task<Created<TimelineStreamEntry>> PostEntryAsync(
        string entityType,
        string entityId,
        PostTimelineEntryRequest request,
        ITimelineStore store,
        ITimelineFollowerService followerService,
        ITimelineNotifier notifier,
        CancellationToken ct)
    {
        TimelineEntry entry = await store.PostEntryAsync(
            entityType, entityId, request.EntryType, request.Body,
            request.ParentEntryId, ct);

        // Parse @mentions and auto-subscribe mentioned users
        IReadOnlyList<string> mentionedUserIds = MentionParser.ExtractMentionedUserIds(entry.Body);
        foreach (string userId in mentionedUserIds)
        {
            await followerService.FollowAsync(userId, entityType, entityId, ct);
        }

        // Notify followers
        IReadOnlyList<string> followerIds = await followerService.GetFollowerIdsAsync(entityType, entityId, ct);
        await notifier.NotifyEntryPostedAsync(entry, followerIds, ct);

        // Notify mentioned users separately (may include extra channels like email)
        if (mentionedUserIds.Count > 0)
        {
            await notifier.NotifyMentionedUsersAsync(entry, mentionedUserIds, ct);
        }

        TimelineStreamEntry result = new()
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
        };

        return TypedResults.Created($"/api/timeline/{entityType}/{entityId}/entries/{entry.Id}", result);
    }

#pragma warning disable S1172 // Route parameters bound by ASP.NET Core minimal API
    private static async Task<Results<NoContent, NotFound>> DeleteEntryAsync(
        string entityType,
        string entityId,
        Guid entryId,
        ITimelineStore store,
        CancellationToken ct)
#pragma warning restore S1172
    {
        try
        {
            await store.DeleteEntryAsync(entryId, ct);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }
}
