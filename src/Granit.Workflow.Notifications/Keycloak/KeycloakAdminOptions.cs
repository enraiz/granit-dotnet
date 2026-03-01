using System.ComponentModel.DataAnnotations;

namespace Granit.Workflow.Notifications.Keycloak;

/// <summary>
/// Configuration options for the Keycloak Admin API used by
/// <see cref="KeycloakApproverResolver"/> to resolve approvers by role membership.
/// </summary>
/// <remarks>
/// Requires a Keycloak service account client with the
/// <c>realm-management:view-users</c> role. Credentials must be loaded from Vault —
/// never stored in plain text.
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
    /// Builds the token endpoint URL for the <c>client_credentials</c> flow.
    /// </summary>
    internal string GetTokenEndpoint() =>
        $"{BaseUrl.TrimEnd('/')}/realms/{Realm}/protocol/openid-connect/token";

    /// <summary>
    /// Builds the Admin API URL for listing users assigned to a realm role.
    /// </summary>
    internal string GetRoleUsersEndpoint(string roleName) =>
        $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}/roles/{Uri.EscapeDataString(roleName)}/users";
}
