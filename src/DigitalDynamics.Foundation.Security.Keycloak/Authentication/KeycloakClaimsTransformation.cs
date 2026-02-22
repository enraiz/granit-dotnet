// =============================================================================
// KeycloakClaimsTransformation - Mapping des rôles Keycloak → ClaimTypes.Role
// =============================================================================
// Keycloak stocke les rôles dans deux emplacements selon la configuration :
//
//   RoleClaimsSource = "realm_access"   (défaut) :
//     { "realm_access": { "roles": ["admin", "practitioner"] } }
//
//   RoleClaimsSource = "resource_access" :
//     { "resource_access": { "guava-backend": { "roles": ["admin"] } } }
//
// Cette transformation extrait les rôles et les ajoute comme ClaimTypes.Role
// standard .NET, permettant [Authorize(Roles = "admin")].
//
// Inputs  : IOptions<KeycloakOptions> (RoleClaimsSource, ClientId)
// Outputs : ClaimsPrincipal enrichi avec ClaimTypes.Role pour chaque rôle Keycloak
// =============================================================================

using System.Security.Claims;
using System.Text.Json;
using DigitalDynamics.Foundation.Security.Keycloak.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Security.Keycloak.Authentication;

/// <summary>
/// Transforme les claims Keycloak pour mapper les rôles
/// vers des <see cref="ClaimTypes.Role"/> standard .NET.
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

        // Pour resource_access, descendre dans le nœud ClientId avant "roles"
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
