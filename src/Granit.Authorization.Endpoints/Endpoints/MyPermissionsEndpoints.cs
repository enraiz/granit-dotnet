using Granit.Authorization.Abstractions;
using Granit.Authorization.Endpoints.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.Authorization.Endpoints.Endpoints;

/// <summary>
/// GET endpoint returning the permissions granted to the current authenticated user.
/// </summary>
internal static class MyPermissionsEndpoints
{
    /// <summary>
    /// Registers GET /me onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapMyPermissionsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/me", GetMyPermissionsAsync)
            .WithName("GetMyPermissions")
            .WithSummary("Returns the list of permissions granted to the current user.")
            .RequireAuthorization();

        return group;
    }

    private static async Task<Ok<MyPermissionsResponse>> GetMyPermissionsAsync(
        IPermissionDefinitionManager definitionManager,
        IPermissionChecker permissionChecker,
        CancellationToken ct)
    {
        IReadOnlyList<PermissionDefinition> allPermissions = definitionManager.GetAll();
        List<string> granted = [];

        foreach (PermissionDefinition permission in allPermissions)
        {
            if (await permissionChecker.IsGrantedAsync(permission.Name, ct).ConfigureAwait(false))
            {
                granted.Add(permission.Name);
            }
        }

        return TypedResults.Ok(new MyPermissionsResponse(granted));
    }
}
