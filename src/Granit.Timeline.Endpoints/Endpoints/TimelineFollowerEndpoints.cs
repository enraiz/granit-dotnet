using Granit.Security;
using Granit.Timeline.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.Timeline.Endpoints.Endpoints;

/// <summary>
/// Endpoints for managing entity followers (follow, unfollow, list).
/// </summary>
internal static class TimelineFollowerEndpoints
{
    internal static RouteGroupBuilder MapFollowerEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{entityType}/{entityId}/follow", FollowAsync)
            .WithName("FollowTimelineEntity")
            .WithSummary("Subscribes the current user as a follower of an entity.");

        group.MapDelete("/{entityType}/{entityId}/follow", UnfollowAsync)
            .WithName("UnfollowTimelineEntity")
            .WithSummary("Unsubscribes the current user from an entity.");

        group.MapGet("/{entityType}/{entityId}/followers", GetFollowersAsync)
            .WithName("GetTimelineFollowers")
            .WithSummary("Returns the user IDs of all followers of an entity.");

        return group;
    }

    private static async Task<NoContent> FollowAsync(
        string entityType,
        string entityId,
        ITimelineFollowerService followerService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        string userId = currentUser.UserId ?? string.Empty;
        await followerService.FollowAsync(userId, entityType, entityId, ct).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> UnfollowAsync(
        string entityType,
        string entityId,
        ITimelineFollowerService followerService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        string userId = currentUser.UserId ?? string.Empty;
        await followerService.UnfollowAsync(userId, entityType, entityId, ct).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<string>>> GetFollowersAsync(
        string entityType,
        string entityId,
        ITimelineFollowerService followerService,
        CancellationToken ct)
    {
        IReadOnlyList<string> followers = await followerService.GetFollowerIdsAsync(entityType, entityId, ct).ConfigureAwait(false);
        return TypedResults.Ok(followers);
    }
}
