// =============================================================================
// KeycloakOptions - Keycloak OIDC authentication configuration
// =============================================================================
// Bound from the "Keycloak" section of configuration (IOptions<T> pattern).
//
// Example appsettings.json:
//   "Keycloak": {
//     "Authority": "https://keycloak.example.com/realms/my-realm",
//     "ClientId": "my-client",
//     "ClientSecret": "***",
//     "RequireHttpsMetadata": true,
//     "AdminRole": "admin",
//     "RoleClaimsSource": "realm_access"
//   }
// =============================================================================

namespace DigitalDynamics.Foundation.Authentication.Keycloak.Options;

/// <summary>
/// Configuration options for Keycloak OIDC authentication.
/// </summary>
public sealed class KeycloakOptions
{
    /// <summary>Section key in the configuration.</summary>
    public const string SectionName = "Keycloak";

    /// <summary>OIDC authority URL (e.g. https://keycloak.example.com/realms/my-realm).</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Keycloak client ID (e.g. guava-backend).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client secret (confidential — load from Vault, never in plain text).</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Require HTTPS for OIDC metadata. Default: true in production.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Expected audience in the token. Default: ClientId.</summary>
    public string? Audience { get; set; }

    /// <summary>Admin role (Keycloak realm role). Default: "admin".</summary>
    public string AdminRole { get; set; } = "admin";

    /// <summary>
    /// Source of roles in the Keycloak token.
    /// <list type="bullet">
    /// <item><c>"realm_access"</c> (default): realm roles (<c>realm_access.roles</c>)</item>
    /// <item><c>"resource_access"</c>: client roles (<c>resource_access.{ClientId}.roles</c>)</item>
    /// </list>
    /// </summary>
    public string RoleClaimsSource { get; set; } = "realm_access";
}
