using Granit.Authentication.ApiKeys.Endpoints.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.Authentication.ApiKeys.Endpoints.Endpoints;

/// <summary>
/// Endpoint for updating API key permissions and CIDR scopes.
/// </summary>
internal static class ApiKeyScopesEndpoints
{
    internal static RouteGroupBuilder MapScopesEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}/scopes", UpdateScopesAsync)
            .WithName("UpdateApiKeyScopes")
            .WithSummary("Updates the permissions and allowed CIDR ranges for an API key.");

        return group;
    }

    private static async Task<Results<NoContent, NotFound>> UpdateScopesAsync(
        Guid id,
        ApiKeyUpdateScopesRequest request,
        IApiKeyAdminStore adminStore,
        CancellationToken cancellationToken)
    {
        bool updated = await adminStore.UpdateScopesAsync(
            id,
            request.Permissions,
            request.AllowedCidrs,
            cancellationToken).ConfigureAwait(false);

        if (!updated)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}
