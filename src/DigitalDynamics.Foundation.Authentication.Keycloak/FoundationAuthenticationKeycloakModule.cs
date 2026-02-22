// =============================================================================
// FoundationAuthenticationKeycloakModule - Keycloak module for Foundation.Authentication
// =============================================================================
// Depends on FoundationJwtBearerModule (generic JWT Bearer).
// Overrides the JWT Bearer config with values from the "Keycloak" section:
//   Authority, Audience (or ClientId), NameClaimType = "preferred_username"
// Registers KeycloakClaimsTransformation (realm_access.roles -> ClaimTypes.Role)
// and the "Admin" policy (role configured in KeycloakOptions.AdminRole).
//
// Migrating from FoundationJwtBearerModule:
//   Replace [DependsOn(typeof(FoundationJwtBearerModule))]
//      with [DependsOn(typeof(FoundationAuthenticationKeycloakModule))]
//   The "Keycloak" section in appsettings.json remains unchanged.
// =============================================================================

using DigitalDynamics.Foundation.Authentication.JwtBearer;
using DigitalDynamics.Foundation.Authentication.Keycloak.Extensions;
using DigitalDynamics.Foundation.Core.Modularity;

namespace DigitalDynamics.Foundation.Authentication.Keycloak;

/// <summary>
/// Foundation module for Keycloak extras (claims transformation, Admin policy).
/// Depends on <see cref="FoundationJwtBearerModule"/> for generic JWT Bearer.
/// </summary>
[DependsOn(typeof(FoundationJwtBearerModule))]
public sealed class FoundationAuthenticationKeycloakModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationKeycloak(context.Configuration);
}
