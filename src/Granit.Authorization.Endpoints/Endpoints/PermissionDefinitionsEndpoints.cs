using Granit.Authorization.Abstractions;
using Granit.Authorization.Endpoints.Dtos;
using Granit.Authorization.Endpoints.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.Authorization.Endpoints.Endpoints;

/// <summary>
/// GET endpoint returning all registered permission definitions grouped by category.
/// </summary>
internal static class PermissionDefinitionsEndpoints
{
    /// <summary>
    /// Registers GET /definitions onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapPermissionDefinitionsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/definitions", GetDefinitions)
            .WithName("GetPermissionDefinitions")
            .WithSummary("Returns all registered permission definitions grouped by category.")
            .RequireAuthorization(AuthorizationEndpointsPermissions.Definitions.Read);

        return group;
    }

    private static Ok<IReadOnlyList<PermissionGroupResponse>> GetDefinitions(
        IPermissionDefinitionManager definitionManager)
    {
        IReadOnlyList<PermissionGroup> groups = definitionManager.GetGroups();

        var response = groups
            .Select(g => new PermissionGroupResponse(
                g.Name,
                g.DisplayName,
                g.Permissions
                    .Select(p => new PermissionDefinitionResponse(p.Name, p.DisplayName))
                    .ToList()))
            .ToList();

        return TypedResults.Ok<IReadOnlyList<PermissionGroupResponse>>(response);
    }
}
