// =============================================================================
// KeycloakClaimsTransformation - Mapping Keycloak roles -> ClaimTypes.Role
// =============================================================================
// Keycloak stores roles in two locations depending on configuration:
//
//   RoleClaimsSource = "realm_access"   (default):
//     { "realm_access": { "roles": ["admin", "practitioner"] } }
//
//   RoleClaimsSource = "resource_access":
//     { "resource_access": { "my-client": { "roles": ["admin"] } } }
//
// This transformation extracts the roles and adds them as standard
// .NET ClaimTypes.Role, enabling [Authorize(Roles = "admin")].
//
// Inputs  : IOptions<KeycloakOptions> (RoleClaimsSource, ClientId)
// Outputs : ClaimsPrincipal enriched with ClaimTypes.Role for each Keycloak role
// =============================================================================

using System.Security.Claims;
using System.Text.Json;
using DigitalDynamics.Foundation.Authentication.Keycloak.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Authentication.Keycloak.Authentication;

/// <summary>
/// Transforms Keycloak claims to map roles
/// to standard .NET <see cref="ClaimTypes.Role"/>.
/// </summary>
public sealed class KeycloakClaimsTransformation : IClaimsTransformation
{
    private const string RolesProperty = "roles";

    private readonly IOptions<KeycloakOptions> _options;

    public KeycloakClaimsTransformation(IOptions<KeycloakOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        KeycloakOptions opts = _options.Value;
        Claim? accessClaim = identity.FindFirst(opts.RoleClaimsSource);
        if (accessClaim is null)
        {
            return Task.FromResult(principal);
        }

        using JsonDocument doc = JsonDocument.Parse(accessClaim.Value);
        JsonElement root = doc.RootElement;

        // For resource_access, descend into the ClientId node before "roles"
        if (opts.RoleClaimsSource == "resource_access" && !string.IsNullOrEmpty(opts.ClientId))
        {
            if (!root.TryGetProperty(opts.ClientId, out root))
            {
                return Task.FromResult(principal);
            }
        }

        if (!root.TryGetProperty(RolesProperty, out JsonElement rolesElement))
        {
            return Task.FromResult(principal);
        }

        foreach (JsonElement role in rolesElement.EnumerateArray())
        {
            string? roleValue = role.GetString();
            if (!string.IsNullOrEmpty(roleValue) && !identity.HasClaim(ClaimTypes.Role, roleValue))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
            }
        }

        return Task.FromResult(principal);
    }
}
