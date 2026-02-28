using System.Security.Claims;
using Granit.Core.MultiTenancy;
using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Granit.Timing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Granit.Notifications.Endpoints.Extensions;

/// <summary>
/// Extension methods for mapping Granit.Notifications REST endpoints.
/// </summary>
public static class NotificationEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps all Granit.Notifications REST endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapGranitNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/notifications")
            .RequireAuthorization()
            .WithTags("Notifications");

        MapInboxEndpoints(group);
        MapActivityFeedEndpoints(group);
        MapPreferenceEndpoints(group);
        MapSubscriptionEndpoints(group);
        MapEntityFollowerEndpoints(group);

        return endpoints;
    }

    private static void MapInboxEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            IUserNotificationStore store,
            ICurrentTenant tenant,
            ClaimsPrincipal user,
            int skip = 0, int take = 20) =>
        {
            string userId = GetUserId(user);
            Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
            IReadOnlyList<UserNotification> notifications = await store.GetListAsync(userId, tenantId, skip, take);
            return Results.Ok(notifications);
        }).WithName("GetNotifications");

        group.MapGet("/unread/count", async (
            IUserNotificationStore store,
            ICurrentTenant tenant,
            ClaimsPrincipal user) =>
        {
            string userId = GetUserId(user);
            Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
            int count = await store.GetUnreadCountAsync(userId, tenantId);
            return Results.Ok(new { count });
        }).WithName("GetUnreadCount");

        group.MapPost("/{id:guid}/read", async (
            Guid id,
            IUserNotificationStore store,
            IClock clock) =>
        {
            await store.MarkAsReadAsync(id, clock.Now);
            return Results.NoContent();
        }).WithName("MarkAsRead");

        group.MapPost("/read-all", async (
            IUserNotificationStore store,
            ICurrentTenant tenant,
            ClaimsPrincipal user,
            IClock clock) =>
        {
            string userId = GetUserId(user);
            Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
            await store.MarkAllAsReadAsync(userId, tenantId, clock.Now);
            return Results.NoContent();
        }).WithName("MarkAllAsRead");
    }

    private static void MapActivityFeedEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/entity/{entityType}/{entityId}", async (
            string entityType,
            string entityId,
            IUserNotificationStore store,
            ICurrentTenant tenant,
            int skip = 0, int take = 20) =>
        {
            Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
            IReadOnlyList<UserNotification> notifications = await store.GetByEntityAsync(entityType, entityId, tenantId, skip, take);
            return Results.Ok(notifications);
        }).WithName("GetEntityActivityFeed");
    }

    private static void MapPreferenceEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/preferences", async (
            INotificationPreferenceStore store,
            ICurrentTenant tenant,
            ClaimsPrincipal user) =>
        {
            string userId = GetUserId(user);
            Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
            IReadOnlyList<NotificationPreference> preferences = await store.GetListAsync(userId, tenantId);
            return Results.Ok(preferences);
        }).WithName("GetPreferences");

        group.MapPut("/preferences", async (
            UpdatePreferenceRequest request,
            INotificationPreferenceStore store,
            ICurrentTenant tenant,
            ClaimsPrincipal user,
            IClock clock) =>
        {
            string userId = GetUserId(user);
            Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
            NotificationPreference preference = new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NotificationTypeName = request.NotificationTypeName,
                ChannelName = request.ChannelName,
                IsEnabled = request.IsEnabled,
                TenantId = tenantId,
                CreatedAt = clock.Now,
                CreatedBy = userId,
                ModifiedAt = clock.Now,
                ModifiedBy = userId,
            };
            await store.SetAsync(preference);
            return Results.NoContent();
        }).WithName("UpdatePreference");

        group.MapGet("/types", (INotificationDefinitionStore definitionStore) =>
        {
            IReadOnlyList<NotificationDefinition> definitions = definitionStore.GetAll();
            return Results.Ok(definitions);
        }).WithName("GetNotificationTypes");
    }

    private static void MapSubscriptionEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/subscriptions", async (
            INotificationSubscriptionStore store,
            ICurrentTenant tenant,
            ClaimsPrincipal user) =>
        {
            string userId = GetUserId(user);
            Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
            IReadOnlyList<NotificationSubscription> subscriptions = await store.GetUserSubscriptionsAsync(userId, tenantId);
            return Results.Ok(subscriptions);
        }).WithName("GetSubscriptions");

        group.MapPost("/subscriptions/{typeName}", async (
            string typeName,
            INotificationSubscriptionStore store,
            ICurrentTenant tenant,
            ClaimsPrincipal user) =>
        {
            string userId = GetUserId(user);
            Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
            await store.SubscribeAsync(userId, typeName, tenantId);
            return Results.NoContent();
        }).WithName("Subscribe");

        group.MapDelete("/subscriptions/{typeName}", async (
            string typeName,
            INotificationSubscriptionStore store,
            ICurrentTenant tenant,
            ClaimsPrincipal user) =>
        {
            string userId = GetUserId(user);
            Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
            await store.UnsubscribeAsync(userId, typeName, tenantId);
            return Results.NoContent();
        }).WithName("Unsubscribe");
    }

    private static void MapEntityFollowerEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/entity/{entityType}/{entityId}/follow", async (
            string entityType,
            string entityId,
            INotificationSubscriptionStore store,
            ICurrentTenant tenant,
            ClaimsPrincipal user) =>
        {
            string userId = GetUserId(user);
            Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
            await store.FollowEntityAsync(userId, entityType, entityId, tenantId);
            return Results.NoContent();
        }).WithName("FollowEntity");

        group.MapDelete("/entity/{entityType}/{entityId}/follow", async (
            string entityType,
            string entityId,
            INotificationSubscriptionStore store,
            ICurrentTenant tenant,
            ClaimsPrincipal user) =>
        {
            string userId = GetUserId(user);
            Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
            await store.UnfollowEntityAsync(userId, entityType, entityId, tenantId);
            return Results.NoContent();
        }).WithName("UnfollowEntity");

        group.MapGet("/entity/{entityType}/{entityId}/followers", async (
            string entityType,
            string entityId,
            INotificationSubscriptionStore store,
            ICurrentTenant tenant) =>
        {
            Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
            IReadOnlyList<NotificationSubscription> followers = await store.GetEntityFollowersAsync(entityType, entityId, tenantId);
            return Results.Ok(followers);
        }).WithName("GetEntityFollowers");
    }

    private static string GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException("User identifier claim not found.");
}
