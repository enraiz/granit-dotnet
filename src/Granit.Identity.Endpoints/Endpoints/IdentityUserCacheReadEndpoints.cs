using Granit.Identity.Endpoints.Dtos;
using Granit.Identity.Models;
using Granit.Querying;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.Identity.Endpoints.Endpoints;

/// <summary>
/// Read endpoints for the identity user cache (list, get, batch).
/// </summary>
internal static class IdentityUserCacheReadEndpoints
{
    internal static RouteGroupBuilder MapReadEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", SearchAsync)
            .WithName("SearchIdentityUserCache")
            .WithSummary("Searches the user cache by free-text term with pagination.");

        group.MapGet("/{userId}", GetByIdAsync)
            .WithName("GetIdentityUserById")
            .WithSummary("Resolves a single user by external ID (cache-aside: fetches from provider if stale/missing).");

        group.MapPost("/batch", BatchResolveAsync)
            .WithName("BatchResolveIdentityUsers")
            .WithSummary("Resolves multiple user IDs to identity information in batch.");

        return group;
    }

    private static async Task<Ok<PagedResult<IdentityUser>>> SearchAsync(
        IUserLookupService lookupService,
        [AsParameters] IdentityUserCacheListRequest request,
        CancellationToken ct)
    {
        int clampedPage = Math.Max(request.Page, 1);
        int clampedPageSize = Math.Clamp(request.PageSize, 1, QueryingDefaults.MaxPageSize);

        PagedResult<IdentityUser> result = await lookupService.SearchAsync(
            request.Search ?? "",
            clampedPage,
            clampedPageSize,
            ct).ConfigureAwait(false);

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<IdentityUser>, NotFound>> GetByIdAsync(
        string userId,
        IUserLookupService lookupService,
        CancellationToken ct)
    {
        IdentityUser? user = await lookupService.FindByIdAsync(userId, ct).ConfigureAwait(false);

        if (user is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(user);
    }

    private static async Task<Ok<IReadOnlyList<IdentityUser>>> BatchResolveAsync(
        IdentityUserCacheBatchRequest request,
        IUserLookupService lookupService,
        CancellationToken ct)
    {
        IReadOnlyList<IdentityUser> users = await lookupService.FindByIdsAsync(
            request.UserIds, ct).ConfigureAwait(false);

        return TypedResults.Ok(users);
    }
}
