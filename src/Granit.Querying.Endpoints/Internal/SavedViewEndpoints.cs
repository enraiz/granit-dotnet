using System.Security.Claims;
using Granit.Core.MultiTenancy;
using Granit.Querying.SavedViews;
using Granit.Validation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.Querying.Endpoints.Internal;

/// <summary>
/// CRUD endpoints for saved views.
/// </summary>
internal static class SavedViewEndpoints
{
    /// <summary>
    /// Registers saved view endpoints on the route group:
    /// GET /saved-views, POST /saved-views, PUT /saved-views/{id},
    /// DELETE /saved-views/{id}, POST /saved-views/{id}/set-default.
    /// </summary>
    internal static void MapSavedViewEndpoints(
        this RouteGroupBuilder group, string entityType)
    {
        RouteGroupBuilder savedViews = group.MapGroup("/saved-views");

        savedViews.MapGet("/", (
            ISavedViewStore store,
            ICurrentTenant tenant,
            ClaimsPrincipal user,
            CancellationToken ct) =>
            GetListAsync(store, entityType, tenant, user, ct))
            .WithName($"GetSavedViews_{entityType}")
            .WithSummary("Returns saved views for the current user.");

        savedViews.MapPost("/", (
            CreateSavedViewRequest request,
            ISavedViewStore store,
            ICurrentTenant tenant,
            ClaimsPrincipal user,
            CancellationToken ct) =>
            CreateAsync(request, store, entityType, tenant, user, ct))
            .WithName($"CreateSavedView_{entityType}")
            .WithSummary("Creates a new saved view.")
            .ValidateBody<CreateSavedViewRequest>();

        savedViews.MapPut("/{id:guid}", (
            Guid id,
            UpdateSavedViewRequest request,
            ISavedViewStore store,
            CancellationToken ct) =>
            UpdateAsync(id, request, store, ct))
            .WithName($"UpdateSavedView_{entityType}")
            .WithSummary("Updates an existing saved view.")
            .ValidateBody<UpdateSavedViewRequest>();

        savedViews.MapDelete("/{id:guid}", (
            Guid id,
            ISavedViewStore store,
            CancellationToken ct) =>
            DeleteAsync(id, store, ct))
            .WithName($"DeleteSavedView_{entityType}")
            .WithSummary("Deletes a saved view.");

        savedViews.MapPost("/{id:guid}/set-default", (
            Guid id,
            ISavedViewStore store,
            ClaimsPrincipal user,
            CancellationToken ct) =>
            SetDefaultAsync(id, store, entityType, user, ct))
            .WithName($"SetDefaultSavedView_{entityType}")
            .WithSummary("Sets a saved view as the default for the current user.");
    }

    private static async Task<Ok<List<SavedViewResponse>>> GetListAsync(
        ISavedViewStore store,
        string entityType,
        ICurrentTenant tenant,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        string userId = GetUserId(user);
        Guid? tenantId = tenant.IsAvailable ? tenant.Id : null;

        IReadOnlyList<SavedView> views = await store
            .GetListAsync(entityType, userId, tenantId, ct)
            .ConfigureAwait(false);

        return TypedResults.Ok(views.Select(MapView).ToList());
    }

    private static async Task<Created<SavedViewResponse>> CreateAsync(
        CreateSavedViewRequest request,
        ISavedViewStore store,
        string entityType,
        ICurrentTenant tenant,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        string userId = GetUserId(user);

        SavedView view = new()
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            Name = request.Name,
            UserId = userId,
            IsShared = request.IsShared,
            IsDefault = request.IsDefault,
            FilterJson = request.FilterJson,
            SortJson = request.SortJson,
            GroupByJson = request.GroupByJson,
            VisibleColumnsJson = request.VisibleColumnsJson,
            TenantId = tenant.IsAvailable ? tenant.Id : null,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId,
        };

        await store.CreateAsync(view, ct).ConfigureAwait(false);
        return TypedResults.Created($"/saved-views/{view.Id}", MapView(view));
    }

    private static async Task<Results<NoContent, NotFound>> UpdateAsync(
        Guid id,
        UpdateSavedViewRequest request,
        ISavedViewStore store,
        CancellationToken ct)
    {
        SavedView? existing = await store.GetAsync(id, ct).ConfigureAwait(false);
        if (existing is null)
        {
            return TypedResults.NotFound();
        }

        existing.Name = request.Name;
        existing.IsShared = request.IsShared;
        existing.FilterJson = request.FilterJson;
        existing.SortJson = request.SortJson;
        existing.GroupByJson = request.GroupByJson;
        existing.VisibleColumnsJson = request.VisibleColumnsJson;
        existing.ModifiedAt = DateTimeOffset.UtcNow;

        await store.UpdateAsync(existing, ct).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteAsync(
        Guid id,
        ISavedViewStore store,
        CancellationToken ct)
    {
        await store.DeleteAsync(id, ct).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> SetDefaultAsync(
        Guid id,
        ISavedViewStore store,
        string entityType,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        string userId = GetUserId(user);
        await store.SetDefaultAsync(id, userId, entityType, ct).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static SavedViewResponse MapView(SavedView v) =>
        new(v.Id, v.EntityType, v.Name, v.UserId, v.IsShared, v.IsDefault,
            v.FilterJson, v.SortJson, v.GroupByJson, v.VisibleColumnsJson);

    private static string GetUserId(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? user.FindFirst("sub")?.Value
        ?? string.Empty;
}
