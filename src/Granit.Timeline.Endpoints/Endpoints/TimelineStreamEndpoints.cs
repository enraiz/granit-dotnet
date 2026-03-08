using Granit.Querying;
using Granit.Timeline.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.Timeline.Endpoints.Endpoints;

/// <summary>
/// GET endpoints for querying the paginated activity stream.
/// </summary>
internal static class TimelineStreamEndpoints
{
    internal static RouteGroupBuilder MapStreamEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{entityType}/{entityId}", GetStreamAsync)
            .WithName("GetTimelineStream")
            .WithSummary("Returns the paginated activity stream for an entity, newest first.");

        return group;
    }

    private static async Task<Ok<PagedResult<TimelineStreamEntry>>> GetStreamAsync(
        string entityType,
        string entityId,
        ITimelineQuery query,
        int page = 1,
        int pageSize = QueryingDefaults.DefaultPageSize,
        CancellationToken ct = default)
    {
        PagedResult<TimelineStreamEntry> result = await query.GetStreamAsync(entityType, entityId, page, pageSize, ct).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }
}
