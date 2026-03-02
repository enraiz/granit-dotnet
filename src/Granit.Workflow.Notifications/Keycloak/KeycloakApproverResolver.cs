using System.Net.Http.Headers;
using System.Net.Http.Json;
using Granit.Authorization.Abstractions;
using Granit.Core.MultiTenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.Workflow.Notifications.Keycloak;

/// <summary>
/// Resolves workflow approvers by querying the Keycloak Admin API for users
/// assigned to the roles that hold the required permission.
/// </summary>
/// <remarks>
/// <para>
/// Resolution flow:
/// <list type="number">
///   <item>
///     <see cref="IPermissionManager.GetGrantedRolesAsync"/> retrieves role names
///     granted the required permission (from the authorization database).
///   </item>
///   <item>
///     For each role, the Keycloak Admin API
///     (<c>GET /admin/realms/{realm}/roles/{role}/users</c>) returns the members.
///   </item>
///   <item>User IDs are aggregated and deduplicated.</item>
/// </list>
/// </para>
/// <para>
/// Follows graceful degradation: if Keycloak is unreachable, logs a warning and
/// returns an empty list instead of propagating the exception.
/// </para>
/// </remarks>
internal sealed class KeycloakApproverResolver(
    IPermissionManager permissionManager,
    KeycloakAdminTokenService tokenService,
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakAdminOptions> options,
    ICurrentTenant currentTenant,
    ILogger<KeycloakApproverResolver> logger) : IApproverResolver
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ResolveApproversAsync(
        string requiredPermission,
        CancellationToken cancellationToken = default)
    {
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;

        IReadOnlyList<string> roles = await permissionManager.GetGrantedRolesAsync(
            requiredPermission, tenantId, cancellationToken).ConfigureAwait(false);

        if (roles.Count == 0)
        {
            logger.LogDebug(
                "No roles found for permission {Permission} (tenant: {TenantId})",
                requiredPermission, tenantId);
            return [];
        }

        try
        {
            string token = await tokenService.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            HttpClient client = httpClientFactory.CreateClient("KeycloakAdmin");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            HashSet<string> userIds = [];

            foreach (string role in roles)
            {
                List<KeycloakUserRepresentation>? users = await GetRoleMembersAsync(
                    client, role, cancellationToken).ConfigureAwait(false);

                if (users is not null)
                {
                    foreach (KeycloakUserRepresentation user in users)
                    {
                        userIds.Add(user.Id);
                    }
                }
            }

            logger.LogDebug(
                "Resolved {UserCount} approvers for permission {Permission} across {RoleCount} roles",
                userIds.Count, requiredPermission, roles.Count);

            return [.. userIds];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to resolve approvers from Keycloak for permission {Permission}. " +
                "Returning empty list (graceful degradation)",
                requiredPermission);
            return [];
        }
    }

    private async Task<List<KeycloakUserRepresentation>?> GetRoleMembersAsync(
        HttpClient client,
        string roleName,
        CancellationToken cancellationToken)
    {
        string endpoint = options.Value.GetRoleUsersEndpoint(roleName);

        HttpResponseMessage response = await client.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<List<KeycloakUserRepresentation>>(cancellationToken).ConfigureAwait(false);
    }
}
