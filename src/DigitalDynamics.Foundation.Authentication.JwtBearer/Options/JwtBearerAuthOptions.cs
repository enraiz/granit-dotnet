// =============================================================================
// JwtBearerAuthOptions - Generic JWT Bearer (OIDC) configuration
// =============================================================================
// Bound from the "Authentication" section of configuration (IOptions<T> pattern).
//
// Example appsettings.json:
//   "Authentication": {
//     "Authority": "https://idp.example.com/realms/myrealm",
//     "Audience": "my-client",
//     "RequireHttpsMetadata": true,
//     "NameClaimType": "sub"
//   }
//
// For Keycloak: use FoundationAuthenticationKeycloakModule
// (reads the "Keycloak" section, reconfigures JWT Bearer via PostConfigure).
// =============================================================================

namespace DigitalDynamics.Foundation.Authentication.JwtBearer.Options;

/// <summary>
/// Configuration options for generic OIDC-compatible JWT Bearer authentication.
/// </summary>
public sealed class JwtBearerAuthOptions
{
    /// <summary>Section key in the configuration.</summary>
    public const string SectionName = "Authentication";

    /// <summary>OIDC authority URL (e.g. https://idp.example.com/realms/myrealm).</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Expected audience in the token.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Require HTTPS for OIDC metadata. Default: true in production.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Claim used as the username (<see cref="System.Security.Principal.IIdentity.Name"/>).
    /// Default: <c>"sub"</c> (RFC 7519 — mandatory claim, always present in a valid JWT).
    /// </summary>
    public string NameClaimType { get; set; } = "sub";
}
