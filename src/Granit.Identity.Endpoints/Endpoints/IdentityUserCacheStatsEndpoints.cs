using Granit.Identity.Endpoints.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.Identity.Endpoints.Endpoints;

/// <summary>
/// Stats endpoint for the identity user cache.
/// </summary>
internal static class IdentityUserCacheStatsEndpoints
{
    internal static RouteGroupBuilder MapStatsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/stats", GetStatsAsync)
            .WithName("GetIdentityUserCacheStats")
            .WithSummary("Returns cache statistics: total entries, stale count, oldest/newest sync timestamps.");

        return group;
    }

    private static async Task<Ok<IdentityUserCacheStatsResponse>> GetStatsAsync(
        IUserCacheStats cacheStats,
        CancellationToken ct)
    {
        int total = await cacheStats.GetCountAsync(ct).ConfigureAwait(false);
        int stale = await cacheStats.GetStaleCountAsync(ct).ConfigureAwait(false);
        (DateTimeOffset? oldest, DateTimeOffset? newest) = await cacheStats.GetSyncRangeAsync(ct)
            .ConfigureAwait(false);

        return TypedResults.Ok(new IdentityUserCacheStatsResponse(total, stale, oldest, newest));
    }
}
