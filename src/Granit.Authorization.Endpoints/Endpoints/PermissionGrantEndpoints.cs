using Granit.Authorization.Abstractions;
using Granit.Authorization.Endpoints.Dtos;
using Granit.Authorization.Endpoints.Permissions;
using Granit.Core.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.Authorization.Endpoints.Endpoints;

/// <summary>
/// Admin endpoints for viewing and managing role → permission grants.
/// </summary>
internal static class PermissionGrantEndpoints
{
    /// <summary>
    /// Registers GET /roles/{roleName}, PUT /roles/{roleName}/{permissionName},
    /// and DELETE /roles/{roleName}/{permissionName} onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapPermissionGrantEndpoints(this RouteGroupBuilder group)
    {
        RouteGroupBuilder adminGroup = group.MapGroup("/roles")
            .RequireAuthorization(AuthorizationEndpointsPermissions.Grants.Manage);

        adminGroup.MapGet("/{roleName}", GetGrantedPermissionsAsync)
            .WithName("GetRolePermissions")
            .WithSummary("Returns the list of permissions explicitly granted to a role.");

        adminGroup.MapPut("/{roleName}/{permissionName}", GrantPermissionAsync)
            .WithName("GrantPermission")
            .WithSummary("Grants a permission to a role. No-op if already granted.");

        adminGroup.MapDelete("/{roleName}/{permissionName}", RevokePermissionAsync)
            .WithName("RevokePermission")
            .WithSummary("Revokes a permission from a role. No-op if not granted.");

        return group;
    }

    private static async Task<Ok<PermissionGrantResponse>> GetGrantedPermissionsAsync(
        string roleName,
        IPermissionManagerReader permissionManagerReader,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;

        IReadOnlyList<string> permissions = await permissionManagerReader
            .GetGrantedPermissionsAsync(roleName, tenantId, ct)
            .ConfigureAwait(false);

        return TypedResults.Ok(new PermissionGrantResponse(roleName, permissions));
    }

    private static async Task<Results<NoContent, ValidationProblem>> GrantPermissionAsync(
        string roleName,
        string permissionName,
        IPermissionManagerWriter permissionManagerWriter,
        IPermissionDefinitionManager definitionManager,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        if (!definitionManager.Exists(permissionName))
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["permissionName"] = [$"Permission '{permissionName}' is not defined."]
                });
        }

        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;

        await permissionManagerWriter.SetAsync(permissionName, roleName, tenantId, isGranted: true, ct)
            .ConfigureAwait(false);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ValidationProblem>> RevokePermissionAsync(
        string roleName,
        string permissionName,
        IPermissionManagerWriter permissionManagerWriter,
        IPermissionDefinitionManager definitionManager,
        ICurrentTenant currentTenant,
        CancellationToken ct)
    {
        if (!definitionManager.Exists(permissionName))
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["permissionName"] = [$"Permission '{permissionName}' is not defined."]
                });
        }

        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;

        await permissionManagerWriter.SetAsync(permissionName, roleName, tenantId, isGranted: false, ct)
            .ConfigureAwait(false);

        return TypedResults.NoContent();
    }
}
