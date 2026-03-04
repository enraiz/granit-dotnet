using System.Net.Http.Headers;
using System.Net.Http.Json;
using Granit.Identity.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.Identity.Keycloak.Internal;

/// <summary>
/// <see cref="IIdentityProvider"/> implementation that queries the Keycloak Admin REST API.
/// </summary>
/// <remarks>
/// Follows graceful degradation: if Keycloak is unreachable, logs a warning and
/// returns empty results instead of propagating the exception.
/// </remarks>
internal sealed class KeycloakIdentityProvider(
    KeycloakAdminTokenService tokenService,
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakAdminOptions> options,
    ILogger<KeycloakIdentityProvider> logger) : IIdentityProvider
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<IdentityUser>> GetUsersAsync(
        string? search = null,
        int? first = null,
        int? max = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
            string endpoint = options.Value.GetUsersEndpoint(search, first, max);

            List<KeycloakUserRepresentation>? users = await client
                .GetFromJsonAsync<List<KeycloakUserRepresentation>>(endpoint, cancellationToken)
                .ConfigureAwait(false);

            return users?.ConvertAll(ToIdentityUser) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Failed to get users from Keycloak. Returning empty list");
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<IdentityUser?> GetUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        try
        {
            HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
            string endpoint = options.Value.GetUserEndpoint(userId);

            KeycloakUserRepresentation? user = await client
                .GetFromJsonAsync<KeycloakUserRepresentation>(endpoint, cancellationToken)
                .ConfigureAwait(false);

            return user is not null ? ToIdentityUser(user) : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Failed to get user {UserId} from Keycloak. Returning null", userId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IdentityRole>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
            string endpoint = options.Value.GetRolesEndpoint();

            List<KeycloakRoleRepresentation>? roles = await client
                .GetFromJsonAsync<List<KeycloakRoleRepresentation>>(endpoint, cancellationToken)
                .ConfigureAwait(false);

            return roles?.ConvertAll(r => new IdentityRole(r.Id, r.Name, r.Description)) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Failed to get roles from Keycloak. Returning empty list");
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IdentityUser>> GetRoleMembersAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleName);

        try
        {
            HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
            string endpoint = options.Value.GetRoleUsersEndpoint(roleName);

            List<KeycloakUserRepresentation>? users = await client
                .GetFromJsonAsync<List<KeycloakUserRepresentation>>(endpoint, cancellationToken)
                .ConfigureAwait(false);

            return users?.ConvertAll(ToIdentityUser) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to get members of role {RoleName} from Keycloak. Returning empty list",
                roleName);
            return [];
        }
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(CancellationToken cancellationToken)
    {
        string token = await tokenService.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        HttpClient client = httpClientFactory.CreateClient("KeycloakAdmin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static IdentityUser ToIdentityUser(KeycloakUserRepresentation user) =>
        new(user.Id, user.Username, user.Email, user.FirstName, user.LastName, user.Enabled);
}
