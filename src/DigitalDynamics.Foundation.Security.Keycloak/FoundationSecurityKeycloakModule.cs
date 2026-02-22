// =============================================================================
// FoundationSecurityKeycloakModule - Module Keycloak pour Foundation.Security
// =============================================================================
// Dépend de FoundationSecurityModule (JWT Bearer générique).
// Surcharge la config JWT Bearer avec les valeurs de la section "Keycloak" :
//   Authority, Audience (ou ClientId), NameClaimType = "preferred_username"
// Enregistre KeycloakClaimsTransformation (realm_access.roles → ClaimTypes.Role)
// et la policy "Admin" (rôle configuré dans KeycloakOptions.AdminRole).
//
// Migration depuis FoundationSecurityModule :
//   Remplacer [DependsOn(typeof(FoundationSecurityModule))]
//         par [DependsOn(typeof(FoundationSecurityKeycloakModule))]
//   La section "Keycloak" dans appsettings.json reste inchangée.
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Security.Keycloak.Extensions;

namespace DigitalDynamics.Foundation.Security.Keycloak;

/// <summary>
/// Module Foundation pour les extras Keycloak (claims transformation, policy Admin).
/// Dépend de <see cref="FoundationSecurityModule"/> pour le JWT Bearer générique.
/// </summary>
[DependsOn(typeof(FoundationSecurityModule))]
public sealed class FoundationSecurityKeycloakModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationSecurityKeycloak(context.Configuration);
}
