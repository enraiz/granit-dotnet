using System.Security.Claims;
using Granit.Core.MultiTenancy;
using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Granit.Timing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configure">Optional delegate to customize <see cref="NotificationEndpointsOptions"/>.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapGranitNotificationEndpoints(
        this IEndpointRouteBuilder endpoints,
        Action<NotificationEndpointsOptions>? configure = null)
    {
        NotificationEndpointsOptions options = new();
        configure?.Invoke(options);

        string prefix = string.IsNullOrEmpty(options.ApiPrefix)
            ? options.RoutePrefix
            : $"{options.ApiPrefix.TrimEnd('/')}/{options.RoutePrefix.TrimStart('/')}";

        RouteGroupBuilder group = endpoints.MapGroup(prefix)
            .RequireAuthorization()
            .WithTags(options.TagName);

        MapInboxEndpoints(group);
        MapActivityFeedEndpoints(group);
        MapPreferenceEndpoints(group);
        MapSubscriptionEndpoints(group);
        MapEntityFollowerEndpoints(group);

        return endpoints;
    }

    // -------------------------------------------------------------------------
    // Inbox
    // -------------------------------------------------------------------------

    private static void MapInboxEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/", GetNotificationsAsync)
            .WithName("GetNotifications")
            .WithSummary("Returns the user's notification inbox, newest first.");

        group.MapGet("/unread/count", GetUnreadCountAsync)
            .WithName("GetUnreadCount")
            .WithSummary("Returns the number of unread notifications for the current user.");

        group.MapPost("/{id:guid}/read", MarkAsReadAsync)
            .WithName("MarkAsRead")
            .WithSummary("Marks a single notification as read.");

        group.MapPost("/read-all", MarkAllAsReadAsync)
            .WithName("MarkAllAsRead")
            .WithSummary("Marks all notifications as read for the current user.");
    }

    private static async Task<Ok<List<UserNotificationResponse>>> GetNotificationsAsync(
        IUserNotificationStore store,
        ICurrentTenant tenant,
        ClaimsPrincipal user,
        int skip = 0, int take = 20)
    {
        string userId = GetUserId(user);
        Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
        IReadOnlyList<UserNotification> notifications = await store.GetListAsync(userId, tenantId, skip, take).ConfigureAwait(false);
        return TypedResults.Ok(MapNotifications(notifications));
    }

    private static async Task<Ok<UnreadCountResponse>> GetUnreadCountAsync(
        IUserNotificationStore store,
        ICurrentTenant tenant,
        ClaimsPrincipal user)
    {
        string userId = GetUserId(user);
        Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
        int count = await store.GetUnreadCountAsync(userId, tenantId).ConfigureAwait(false);
        return TypedResults.Ok(new UnreadCountResponse(count));
    }

    private static async Task<NoContent> MarkAsReadAsync(
        Guid id,
        IUserNotificationStore store,
        IClock clock)
    {
        await store.MarkAsReadAsync(id, clock.Now).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> MarkAllAsReadAsync(
        IUserNotificationStore store,
        ICurrentTenant tenant,
        ClaimsPrincipal user,
        IClock clock)
    {
        string userId = GetUserId(user);
        Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
        await store.MarkAllAsReadAsync(userId, tenantId, clock.Now).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    // -------------------------------------------------------------------------
    // Activity feed
    // -------------------------------------------------------------------------

    private static void MapActivityFeedEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/entity/{entityType}/{entityId}", GetEntityActivityFeedAsync)
            .WithName("GetEntityActivityFeed")
            .WithSummary("Returns the activity feed for a specific entity.");
    }

    private static async Task<Ok<List<UserNotificationResponse>>> GetEntityActivityFeedAsync(
        string entityType,
        string entityId,
        IUserNotificationStore store,
        ICurrentTenant tenant,
        int skip = 0, int take = 20)
    {
        Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
        IReadOnlyList<UserNotification> notifications = await store.GetByEntityAsync(entityType, entityId, tenantId, skip, take).ConfigureAwait(false);
        return TypedResults.Ok(MapNotifications(notifications));
    }

    // -------------------------------------------------------------------------
    // Preferences
    // -------------------------------------------------------------------------

    private static void MapPreferenceEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/preferences", GetPreferencesAsync)
            .WithName("GetPreferences")
            .WithSummary("Returns notification delivery preferences for the current user.");

        group.MapPut("/preferences", UpdatePreferenceAsync)
            .WithName("UpdatePreference")
            .WithSummary("Creates or updates a notification delivery preference.");

        group.MapGet("/types", GetNotificationTypes)
            .WithName("GetNotificationTypes")
            .WithSummary("Returns all registered notification type definitions.");
    }

    private static async Task<Ok<List<NotificationPreferenceResponse>>> GetPreferencesAsync(
        INotificationPreferenceStore store,
        ICurrentTenant tenant,
        ClaimsPrincipal user)
    {
        string userId = GetUserId(user);
        Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
        IReadOnlyList<NotificationPreference> preferences = await store.GetListAsync(userId, tenantId).ConfigureAwait(false);
        List<NotificationPreferenceResponse> result = preferences
            .Select(p => new NotificationPreferenceResponse(p.Id, p.UserId, p.NotificationTypeName, p.ChannelName, p.IsEnabled))
            .ToList();
        return TypedResults.Ok(result);
    }

    private static async Task<NoContent> UpdatePreferenceAsync(
        UpdatePreferenceRequest request,
        INotificationPreferenceStore store,
        ICurrentTenant tenant,
        ClaimsPrincipal user,
        IClock clock)
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
        await store.SetAsync(preference).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static Ok<IReadOnlyList<NotificationDefinition>> GetNotificationTypes(
        INotificationDefinitionStore definitionStore)
    {
        IReadOnlyList<NotificationDefinition> definitions = definitionStore.GetAll();
        return TypedResults.Ok(definitions);
    }

    // -------------------------------------------------------------------------
    // Subscriptions
    // -------------------------------------------------------------------------

    private static void MapSubscriptionEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/subscriptions", GetSubscriptionsAsync)
            .WithName("GetSubscriptions")
            .WithSummary("Returns all notification subscriptions for the current user.");

        group.MapPost("/subscriptions/{typeName}", SubscribeAsync)
            .WithName("Subscribe")
            .WithSummary("Subscribes the current user to a notification type.");

        group.MapDelete("/subscriptions/{typeName}", UnsubscribeAsync)
            .WithName("Unsubscribe")
            .WithSummary("Unsubscribes the current user from a notification type.");
    }

    private static async Task<Ok<List<NotificationSubscriptionResponse>>> GetSubscriptionsAsync(
        INotificationSubscriptionStore store,
        ICurrentTenant tenant,
        ClaimsPrincipal user)
    {
        string userId = GetUserId(user);
        Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
        IReadOnlyList<NotificationSubscription> subscriptions = await store.GetUserSubscriptionsAsync(userId, tenantId).ConfigureAwait(false);
        return TypedResults.Ok(MapSubscriptions(subscriptions));
    }

    private static async Task<NoContent> SubscribeAsync(
        string typeName,
        INotificationSubscriptionStore store,
        ICurrentTenant tenant,
        ClaimsPrincipal user)
    {
        string userId = GetUserId(user);
        Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
        await store.SubscribeAsync(userId, typeName, tenantId).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> UnsubscribeAsync(
        string typeName,
        INotificationSubscriptionStore store,
        ICurrentTenant tenant,
        ClaimsPrincipal user)
    {
        string userId = GetUserId(user);
        Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
        await store.UnsubscribeAsync(userId, typeName, tenantId).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    // -------------------------------------------------------------------------
    // Entity followers
    // -------------------------------------------------------------------------

    private static void MapEntityFollowerEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/entity/{entityType}/{entityId}/follow", FollowEntityAsync)
            .WithName("FollowEntity")
            .WithSummary("Subscribes the current user as a follower of an entity.");

        group.MapDelete("/entity/{entityType}/{entityId}/follow", UnfollowEntityAsync)
            .WithName("UnfollowEntity")
            .WithSummary("Unsubscribes the current user from an entity.");

        group.MapGet("/entity/{entityType}/{entityId}/followers", GetEntityFollowersAsync)
            .WithName("GetEntityFollowers")
            .WithSummary("Returns all followers of a specific entity.");
    }

    private static async Task<NoContent> FollowEntityAsync(
        string entityType,
        string entityId,
        INotificationSubscriptionStore store,
        ICurrentTenant tenant,
        ClaimsPrincipal user)
    {
        string userId = GetUserId(user);
        Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
        await store.FollowEntityAsync(userId, entityType, entityId, tenantId).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> UnfollowEntityAsync(
        string entityType,
        string entityId,
        INotificationSubscriptionStore store,
        ICurrentTenant tenant,
        ClaimsPrincipal user)
    {
        string userId = GetUserId(user);
        Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
        await store.UnfollowEntityAsync(userId, entityType, entityId, tenantId).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<List<NotificationSubscriptionResponse>>> GetEntityFollowersAsync(
        string entityType,
        string entityId,
        INotificationSubscriptionStore store,
        ICurrentTenant tenant)
    {
        Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;
        IReadOnlyList<NotificationSubscription> followers = await store.GetEntityFollowersAsync(entityType, entityId, tenantId).ConfigureAwait(false);
        return TypedResults.Ok(MapSubscriptions(followers));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static List<UserNotificationResponse> MapNotifications(
        IReadOnlyList<UserNotification> notifications) =>
        notifications.Select(n => new UserNotificationResponse(
            n.Id, n.NotificationId, n.NotificationTypeName, n.Severity,
            n.RecipientUserId, n.Data.ValueKind == System.Text.Json.JsonValueKind.Undefined ? null : n.Data,
            n.State, n.CreatedAt, n.ReadAt, n.RelatedEntityType, n.RelatedEntityId)).ToList();

    private static List<NotificationSubscriptionResponse> MapSubscriptions(
        IReadOnlyList<NotificationSubscription> subscriptions) =>
        subscriptions.Select(s => new NotificationSubscriptionResponse(
            s.Id, s.UserId, s.NotificationTypeName, s.EntityType, s.EntityId)).ToList();

    private static string GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException("User identifier claim not found.");
}
