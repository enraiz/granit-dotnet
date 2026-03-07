using Granit.Identity.Endpoints.Dtos;
using Granit.Identity.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.Identity.Endpoints.Endpoints;

/// <summary>
/// Sync endpoints for forcing refresh from the identity provider.
/// </summary>
internal static class IdentityUserCacheSyncEndpoints
{
    internal static RouteGroupBuilder MapSyncEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/sync", SyncAsync)
            .WithName("SyncIdentityUsers")
            .WithSummary("Forces refresh of specific users from the identity provider.");

        group.MapPost("/sync-all", SyncAllAsync)
            .WithName("SyncAllIdentityUsers")
            .WithSummary("Full sync — fetches all users from the identity provider and upserts the cache.");

        group.MapPost("/sync-stale", SyncStaleAsync)
            .WithName("SyncStaleIdentityUsers")
            .WithSummary("Incremental sync — refreshes only stale cache entries.");

        return group;
    }

    private static async Task<Ok<IReadOnlyList<IdentityUser>>> SyncAsync(
        IdentityUserCacheSyncRequest request,
        IUserLookupService lookupService,
        CancellationToken ct)
    {
        var results = new List<IdentityUser>();

        foreach (string userId in request.UserIds)
        {
            IdentityUser? refreshed = await lookupService.RefreshByIdAsync(userId, ct)
                .ConfigureAwait(false);

            if (refreshed is not null)
            {
                results.Add(refreshed);
            }
        }

        return TypedResults.Ok<IReadOnlyList<IdentityUser>>(results);
    }

    private static async Task<Ok<IdentityUserCacheSyncAllResponse>> SyncAllAsync(
        IUserLookupService lookupService,
        CancellationToken ct)
    {
        int synced = await lookupService.RefreshAllAsync(ct).ConfigureAwait(false);
        return TypedResults.Ok(new IdentityUserCacheSyncAllResponse(synced));
    }

    private static async Task<Ok<IdentityUserCacheSyncStaleResponse>> SyncStaleAsync(
        IUserLookupService lookupService,
        CancellationToken ct)
    {
        int refreshed = await lookupService.RefreshStaleAsync(ct).ConfigureAwait(false);
        return TypedResults.Ok(new IdentityUserCacheSyncStaleResponse(refreshed));
    }
}

/// <summary>Response for the sync-all endpoint.</summary>
/// <param name="SyncedCount">Number of users synchronized.</param>
internal sealed record IdentityUserCacheSyncAllResponse(int SyncedCount);

/// <summary>Response for the sync-stale endpoint.</summary>
/// <param name="RefreshedCount">Number of stale entries refreshed.</param>
internal sealed record IdentityUserCacheSyncStaleResponse(int RefreshedCount);
