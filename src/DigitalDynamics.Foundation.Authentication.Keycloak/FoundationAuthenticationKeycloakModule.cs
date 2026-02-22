// =============================================================================
// FoundationAuthenticationKeycloakModule - Module Keycloak pour Foundation.Authentication
// =============================================================================
// Dépend de FoundationJwtBearerModule (JWT Bearer générique).
// Surcharge la config JWT Bearer avec les valeurs de la section "Keycloak" :
//   Authority, Audience (ou ClientId), NameClaimType = "preferred_username"
// Enregistre KeycloakClaimsTransformation (realm_access.roles → ClaimTypes.Role)
// et la policy "Admin" (rôle configuré dans KeycloakOptions.AdminRole).
//
// Migration depuis FoundationJwtBearerModule :
//   Remplacer [DependsOn(typeof(FoundationJwtBearerModule))]
//         par [DependsOn(typeof(FoundationAuthenticationKeycloakModule))]
//   La section "Keycloak" dans appsettings.json reste inchangée.
// =============================================================================

using DigitalDynamics.Foundation.Authentication.JwtBearer;
using DigitalDynamics.Foundation.Authentication.Keycloak.Extensions;
using DigitalDynamics.Foundation.Core.Modularity;

namespace DigitalDynamics.Foundation.Authentication.Keycloak;

/// <summary>
/// Module Foundation pour les extras Keycloak (claims transformation, policy Admin).
/// Dépend de <see cref="FoundationJwtBearerModule"/> pour le JWT Bearer générique.
/// </summary>
[DependsOn(typeof(FoundationJwtBearerModule))]
public sealed class FoundationAuthenticationKeycloakModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationKeycloak(context.Configuration);
}
