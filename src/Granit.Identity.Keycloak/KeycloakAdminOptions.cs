using System.ComponentModel.DataAnnotations;

namespace Granit.Identity.Keycloak;

/// <summary>
/// Configuration options for the Keycloak Admin API used by
/// <see cref="Internal.KeycloakIdentityProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// Minimum required role: <c>realm-management:view-users</c>.
/// </para>
/// <para>
/// Additional roles depending on features used:
/// <list type="bullet">
///   <item><description><c>realm-management:manage-users</c> — required for <c>SetUserEnabledAsync</c>.</description></item>
///   <item><description><c>realm-management:impersonation</c> + feature <c>admin-fine-grained-authz</c> — required when <see cref="UseTokenExchangeForDeviceActivity"/> is <c>true</c>.</description></item>
/// </list>
/// </para>
/// <para>
/// Credentials must be loaded from Vault — never stored in plain text.
/// </para>
/// </remarks>
public sealed class KeycloakAdminOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "KeycloakAdmin";

    /// <summary>
    /// Keycloak server base URL (e.g. <c>https://keycloak.example.com</c>).
    /// Must not include the realm path.
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Keycloak realm name (e.g. <c>guava-health</c>).
    /// </summary>
    [Required]
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Service account client ID with <c>realm-management:view-users</c> role.
    /// </summary>
    [Required]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Service account client secret. Must be loaded from Vault at runtime.
    /// </summary>
    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c>, <c>GetUserDeviceActivityAsync</c> uses the Keycloak Account API
    /// (<c>GET /realms/{realm}/account/sessions/devices</c>) via OAuth 2.0 token exchange,
    /// which provides device-level details (OS, browser, device type).
    /// </summary>
    /// <remarks>
    /// Requires:
    /// <list type="bullet">
    ///   <item><description>Feature <c>admin-fine-grained-authz</c> enabled on the Keycloak realm.</description></item>
    ///   <item><description>Role <c>realm-management:impersonation</c> assigned to the service account.</description></item>
    /// </list>
    /// When <c>false</c> (default), falls back to the Admin API sessions endpoint — device OS,
    /// browser and device type fields will be <c>null</c>.
    /// </remarks>
    public bool UseTokenExchangeForDeviceActivity { get; set; }

    /// <summary>
    /// Builds the token endpoint URL for the <c>client_credentials</c> flow.
    /// </summary>
    internal string GetTokenEndpoint() =>
        $"{BaseUrl.TrimEnd('/')}/realms/{Realm}/protocol/openid-connect/token";

    /// <summary>
    /// Builds the Admin API URL for listing users assigned to a realm role.
    /// </summary>
    internal string GetRoleUsersEndpoint(string roleName) =>
        $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}/roles/{Uri.EscapeDataString(roleName)}/users";

    /// <summary>
    /// Builds the Admin API URL for listing users with optional search and pagination.
    /// </summary>
    internal string GetUsersEndpoint(string? search = null, int? first = null, int? max = null)
    {
        string baseUrl = $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}/users";
        List<string> queryParams = [];

        if (!string.IsNullOrEmpty(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        }

        if (first.HasValue)
        {
            queryParams.Add($"first={first.Value}");
        }

        if (max.HasValue)
        {
            queryParams.Add($"max={max.Value}");
        }

        return queryParams.Count > 0
            ? $"{baseUrl}?{string.Join('&', queryParams)}"
            : baseUrl;
    }

    /// <summary>
    /// Builds the Admin API URL for getting a single user by ID.
    /// </summary>
    internal string GetUserEndpoint(string userId) =>
        $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}/users/{Uri.EscapeDataString(userId)}";

    /// <summary>
    /// Builds the Admin API URL for listing realm roles.
    /// </summary>
    internal string GetRolesEndpoint() =>
        $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}/roles";

    /// <summary>
    /// Builds the Admin API URL for listing active sessions of a user.
    /// </summary>
    internal string GetUserSessionsEndpoint(string userId) =>
        $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}/users/{Uri.EscapeDataString(userId)}/sessions";

    /// <summary>
    /// Builds the Admin API URL for listing credentials of a user.
    /// </summary>
    internal string GetUserCredentialsEndpoint(string userId) =>
        $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}/users/{Uri.EscapeDataString(userId)}/credentials";

    /// <summary>
    /// Builds the Account API URL for listing device activity of the authenticated user.
    /// The user is identified by the bearer token — use with a token obtained via token exchange.
    /// </summary>
    internal string GetAccountSessionsDevicesEndpoint() =>
        $"{BaseUrl.TrimEnd('/')}/realms/{Realm}/account/sessions/devices";
}
