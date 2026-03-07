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
    public async Task UpdateUserAsync(
        string userId,
        IdentityUserUpdate update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(update);

        HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
        string endpoint = options.Value.GetUserEndpoint(userId);

        // Keycloak PUT /admin/realms/{realm}/users/{id} expects the full UserRepresentation.
        // We first GET the current representation, patch the requested fields, then PUT back.
        KeycloakUserRepresentation? current = await client
            .GetFromJsonAsync<KeycloakUserRepresentation>(endpoint, cancellationToken)
            .ConfigureAwait(false);

        if (current is null)
        {
            throw new HttpRequestException($"User {userId} not found in Keycloak.");
        }

        KeycloakUserRepresentation updated = current with
        {
            Email = update.Email ?? current.Email,
            FirstName = update.FirstName ?? current.FirstName,
            LastName = update.LastName ?? current.LastName,
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            endpoint, updated, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        logger.LogInformation("User {UserId} profile updated in Keycloak", userId);
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

    // ──── Feature 1: User role management ────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IdentityRole>> GetUserRolesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        try
        {
            HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
            string endpoint = options.Value.GetUserRealmRoleMappingsEndpoint(userId);

            List<KeycloakRoleRepresentation>? roles = await client
                .GetFromJsonAsync<List<KeycloakRoleRepresentation>>(endpoint, cancellationToken)
                .ConfigureAwait(false);

            return roles?.ConvertAll(r => new IdentityRole(r.Id, r.Name, r.Description)) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Failed to get roles for user {UserId} from Keycloak. Returning empty list", userId);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task AssignRoleAsync(
        string userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(roleName);

        HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);

        KeycloakRoleRepresentation role = await GetRoleByNameAsync(client, roleName, cancellationToken).ConfigureAwait(false);

        string endpoint = options.Value.GetUserRealmRoleMappingsEndpoint(userId);
        using HttpResponseMessage response = await client
            .PostAsJsonAsync(endpoint, new[] { role }, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        logger.LogInformation("Role {RoleName} assigned to user {UserId} in Keycloak", roleName, userId);
    }

    /// <inheritdoc/>
    public async Task RemoveRoleAsync(
        string userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(roleName);

        HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);

        KeycloakRoleRepresentation role = await GetRoleByNameAsync(client, roleName, cancellationToken).ConfigureAwait(false);

        string endpoint = options.Value.GetUserRealmRoleMappingsEndpoint(userId);

        using HttpRequestMessage request = new(HttpMethod.Delete, endpoint)
        {
            Content = JsonContent.Create(new[] { role })
        };

        using HttpResponseMessage response = await client
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        logger.LogInformation("Role {RoleName} removed from user {UserId} in Keycloak", roleName, userId);
    }

    // ──── Feature 2: Session termination ────

    /// <inheritdoc/>
    public async Task TerminateSessionAsync(
        string userId,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(sessionId);

        HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
        string endpoint = options.Value.GetSessionEndpoint(sessionId);

        using HttpResponseMessage response = await client
            .DeleteAsync(endpoint, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        logger.LogInformation("Session {SessionId} terminated for user {UserId} in Keycloak", sessionId, userId);
    }

    /// <inheritdoc/>
    public async Task TerminateAllSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
        string endpoint = options.Value.GetUserLogoutEndpoint(userId);

        using HttpResponseMessage response = await client
            .PostAsync(endpoint, content: null, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        logger.LogInformation("All sessions terminated for user {UserId} in Keycloak", userId);
    }

    // ──── Feature 3: Password reset ────

    /// <inheritdoc/>
    public async Task SendPasswordResetEmailAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
        string endpoint = options.Value.GetExecuteActionsEmailEndpoint(userId);

        using HttpResponseMessage response = await client
            .PutAsJsonAsync(endpoint, new[] { "UPDATE_PASSWORD" }, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        logger.LogInformation("Password reset email sent for user {UserId} via Keycloak", userId);
    }

    /// <inheritdoc/>
    public async Task SetTemporaryPasswordAsync(
        string userId,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(temporaryPassword);

        HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
        string endpoint = options.Value.GetResetPasswordEndpoint(userId);

        using HttpResponseMessage response = await client.PutAsJsonAsync(
            endpoint,
            new { type = "password", value = temporaryPassword, temporary = true },
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        logger.LogInformation("Temporary password set for user {UserId} in Keycloak", userId);
    }

    // ──── Feature 4: User creation ────

    /// <inheritdoc/>
    public async Task<IdentityUser> CreateUserAsync(
        IdentityUserCreate user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
        string endpoint = options.Value.GetUsersEndpoint();

        var payload = new
        {
            username = user.Username,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            enabled = user.Enabled
        };

        using HttpResponseMessage createResponse = await client
            .PostAsJsonAsync(endpoint, payload, cancellationToken)
            .ConfigureAwait(false);

        createResponse.EnsureSuccessStatusCode();

        // Extract the created user ID from the Location header
        string locationHeader = createResponse.Headers.Location?.AbsolutePath
            ?? throw new InvalidOperationException("Keycloak did not return a Location header after user creation.");

        string createdUserId = locationHeader[(locationHeader.LastIndexOf('/') + 1)..];

        // Set temporary password if provided
        if (!string.IsNullOrEmpty(user.TemporaryPassword))
        {
            await SetTemporaryPasswordAsync(createdUserId, user.TemporaryPassword, cancellationToken)
                .ConfigureAwait(false);
        }

        logger.LogInformation("User {Username} created with ID {UserId} in Keycloak", user.Username, createdUserId);

        return new IdentityUser(
            createdUserId,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Enabled);
    }

    // ──── Feature 5: Group management ────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IdentityGroup>> GetGroupsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
            string endpoint = options.Value.GetGroupsEndpoint();

            List<KeycloakGroupRepresentation>? groups = await client
                .GetFromJsonAsync<List<KeycloakGroupRepresentation>>(endpoint, cancellationToken)
                .ConfigureAwait(false);

            return groups?.ConvertAll(ToIdentityGroup) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Failed to get groups from Keycloak. Returning empty list");
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IdentityGroup>> GetUserGroupsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        try
        {
            HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
            string endpoint = options.Value.GetUserGroupsEndpoint(userId);

            List<KeycloakGroupRepresentation>? groups = await client
                .GetFromJsonAsync<List<KeycloakGroupRepresentation>>(endpoint, cancellationToken)
                .ConfigureAwait(false);

            return groups?.ConvertAll(ToIdentityGroup) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Failed to get groups for user {UserId} from Keycloak. Returning empty list", userId);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task AddUserToGroupAsync(
        string userId,
        string groupId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(groupId);

        HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
        string endpoint = options.Value.GetUserGroupMembershipEndpoint(userId, groupId);

        using HttpResponseMessage response = await client
            .PutAsync(endpoint, content: null, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        logger.LogInformation("User {UserId} added to group {GroupId} in Keycloak", userId, groupId);
    }

    /// <inheritdoc/>
    public async Task RemoveUserFromGroupAsync(
        string userId,
        string groupId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(groupId);

        HttpClient client = await CreateAuthenticatedClientAsync(cancellationToken).ConfigureAwait(false);
        string endpoint = options.Value.GetUserGroupMembershipEndpoint(userId, groupId);

        using HttpResponseMessage response = await client
            .DeleteAsync(endpoint, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        logger.LogInformation("User {UserId} removed from group {GroupId} in Keycloak", userId, groupId);
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
        new(user.Id, user.Username, user.Email, user.FirstName, user.LastName, user.Enabled,
            FlattenAttributes(user.Attributes));

    private static Dictionary<string, string>? FlattenAttributes(
        Dictionary<string, List<string>>? attributes)
    {
        if (attributes is not { Count: > 0 })
        {
            return null;
        }

        Dictionary<string, string> result = new(attributes.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, List<string>> kvp in attributes)
        {
            if (kvp.Value is [var first, ..])
            {
                result[kvp.Key] = first;
            }
        }

        return result.Count > 0 ? result : null;
    }

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

    private async Task<KeycloakRoleRepresentation> GetRoleByNameAsync(
        HttpClient client, string roleName, CancellationToken cancellationToken)
    {
        string endpoint = options.Value.GetRoleByNameEndpoint(roleName);

        return await client
            .GetFromJsonAsync<KeycloakRoleRepresentation>(endpoint, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Role '{roleName}' not found in Keycloak.");
    }

    private static IdentityGroup ToIdentityGroup(KeycloakGroupRepresentation group) =>
        new(
            Id: group.Id,
            Name: group.Name,
            Path: group.Path,
            SubGroups: group.SubGroups?.ConvertAll(ToIdentityGroup) ?? []);
}
