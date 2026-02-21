// =============================================================================
// KeycloakClaimsTransformation - Mapping realm_access.roles → ClaimTypes.Role
// =============================================================================
// Keycloak stocke les rôles dans le claim "realm_access" au format JSON :
//   { "roles": ["admin", "practitioner"] }
//
// Cette transformation extrait les rôles et les ajoute comme ClaimTypes.Role
// pour permettre l'utilisation de [Authorize(Roles = "admin")] standard.
// =============================================================================

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace DigitalDynamics.Foundation.Security.Authentication;

/// <summary>
/// Transforme les claims Keycloak pour mapper realm_access.roles
/// vers des ClaimTypes.Role standard .NET.
/// </summary>
public sealed class KeycloakClaimsTransformation : IClaimsTransformation
{
    private const string RealmAccessClaim = "realm_access";
    private const string RolesProperty = "roles";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        var realmAccessClaim = identity.FindFirst(RealmAccessClaim);
        if (realmAccessClaim is null)
        {
            return Task.FromResult(principal);
        }

        using var doc = JsonDocument.Parse(realmAccessClaim.Value);
        if (!doc.RootElement.TryGetProperty(RolesProperty, out var rolesElement))
        {
            return Task.FromResult(principal);
        }

        foreach (var role in rolesElement.EnumerateArray())
        {
            var roleValue = role.GetString();
            if (!string.IsNullOrEmpty(roleValue) && !identity.HasClaim(ClaimTypes.Role, roleValue))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
            }
        }

        return Task.FromResult(principal);
    }
}
