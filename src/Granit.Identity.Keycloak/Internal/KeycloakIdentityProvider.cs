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
/// <para>
/// Read operations follow graceful degradation: if Keycloak is unreachable, logs a warning
/// and returns empty results instead of propagating the exception.
/// </para>
/// <para>
/// Write operations (<see cref="SetUserEnabledAsync"/>) propagate exceptions so callers can
/// handle failures explicitly.
/// </para>
/// </remarks>
internal sealed class KeycloakIdentityProvider(
    KeycloakAdminTokenService tokenService,
    KeycloakUserTokenExchangeService tokenExchangeService,
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
    public async Task SetUserEnabledAsync(
        string userId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
        string endpoint = options.Value.GetUserEndpoint(userId);

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            endpoint, new { enabled }, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        logger.LogInformation(
            "User {UserId} {Action} in Keycloak",
            userId, enabled ? "enabled" : "disabled");
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IdentitySession>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        try
        {
            HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
            string endpoint = options.Value.GetUserSessionsEndpoint(userId);

            List<KeycloakSessionRepresentation>? sessions = await client
                .GetFromJsonAsync<List<KeycloakSessionRepresentation>>(endpoint, cancellationToken)
                .ConfigureAwait(false);

            return sessions?.ConvertAll(ToIdentitySession) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Failed to get sessions for user {UserId} from Keycloak. Returning empty list", userId);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IdentityDeviceActivity>> GetUserDeviceActivityAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        try
        {
            return options.Value.UseTokenExchangeForDeviceActivity
                ? await GetDeviceActivityViaAccountApiAsync(userId, cancellationToken).ConfigureAwait(false)
                : await GetDeviceActivityViaAdminSessionsAsync(userId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Failed to get device activity for user {UserId} from Keycloak. Returning empty list", userId);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<DateTimeOffset?> GetPasswordChangedAtAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        try
        {
            HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
            string endpoint = options.Value.GetUserCredentialsEndpoint(userId);

            List<KeycloakCredentialRepresentation>? credentials = await client
                .GetFromJsonAsync<List<KeycloakCredentialRepresentation>>(endpoint, cancellationToken)
                .ConfigureAwait(false);

            KeycloakCredentialRepresentation? passwordCred = credentials?
                .Find(c => string.Equals(c.Type, "password", StringComparison.OrdinalIgnoreCase));

            return passwordCred?.CreatedDate is long ms
                ? DateTimeOffset.FromUnixTimeMilliseconds(ms)
                : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Failed to get credentials for user {UserId} from Keycloak. Returning null", userId);
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

    private async Task<IReadOnlyList<IdentityDeviceActivity>> GetDeviceActivityViaAccountApiAsync(
        string userId, CancellationToken cancellationToken)
    {
        string userToken = await tokenExchangeService
            .ExchangeTokenForUserAsync(userId, cancellationToken).ConfigureAwait(false);

        HttpClient client = httpClientFactory.CreateClient("KeycloakAdmin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        string endpoint = options.Value.GetAccountSessionsDevicesEndpoint();

        List<KeycloakDeviceRepresentation>? devices = await client
            .GetFromJsonAsync<List<KeycloakDeviceRepresentation>>(endpoint, cancellationToken)
            .ConfigureAwait(false);

        return devices?.ConvertAll(ToIdentityDeviceActivity) ?? [];
    }

    private async Task<IReadOnlyList<IdentityDeviceActivity>> GetDeviceActivityViaAdminSessionsAsync(
        string userId, CancellationToken cancellationToken)
    {
        HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
        string endpoint = options.Value.GetUserSessionsEndpoint(userId);

        List<KeycloakSessionRepresentation>? sessions = await client
            .GetFromJsonAsync<List<KeycloakSessionRepresentation>>(endpoint, cancellationToken)
            .ConfigureAwait(false);

        return sessions?.ConvertAll(s => new IdentityDeviceActivity(
            IpAddress: s.IpAddress,
            LastAccess: DateTimeOffset.FromUnixTimeMilliseconds(s.LastAccess),
            Device: null,
            Os: null,
            OsVersion: null,
            Browser: null,
            Mobile: false,
            Current: false,
            Sessions: [ToIdentitySession(s)])) ?? [];
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

    private static IdentitySession ToIdentitySession(KeycloakSessionRepresentation session) =>
        new(
            SessionId: session.Id,
            IpAddress: session.IpAddress,
            StartedAt: DateTimeOffset.FromUnixTimeMilliseconds(session.Start),
            LastAccess: DateTimeOffset.FromUnixTimeMilliseconds(session.LastAccess),
            RememberMe: session.RememberMe,
            Clients: session.Clients is { Count: > 0 }
                ? session.Clients.Values.ToList()
                : []);

    private static IdentityDeviceActivity ToIdentityDeviceActivity(KeycloakDeviceRepresentation device) =>
        new(
            IpAddress: device.IpAddress,
            LastAccess: DateTimeOffset.FromUnixTimeMilliseconds(device.LastAccess),
            Device: device.Device,
            Os: device.Os,
            OsVersion: device.OsVersion,
            Browser: device.Browser,
            Mobile: device.Mobile,
            Current: device.Current,
            Sessions: device.Sessions?.ConvertAll(ToIdentitySession) ?? []);
}
