// =============================================================================
// FoundationJwtBearerModule - Module JWT Bearer générique
// =============================================================================
// Configure l'authentification JWT Bearer générique (OIDC-compatible) et
// enregistre ICurrentUserService via HttpContext.
// Lit la section "Authentication" de appsettings.json.
//
// Pour Keycloak : utiliser FoundationAuthenticationKeycloakModule
// (Foundation.Authentication.Keycloak) qui dépend de ce module.
// =============================================================================

using DigitalDynamics.Foundation.Authentication.JwtBearer.Extensions;
using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Security;

namespace DigitalDynamics.Foundation.Authentication.JwtBearer;

/// <summary>
/// Module Foundation pour l'authentification JWT Bearer générique (OIDC).
/// Dépend de <see cref="FoundationSecurityModule"/> pour les abstractions.
/// </summary>
[DependsOn(typeof(FoundationSecurityModule))]
public sealed class FoundationJwtBearerModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationJwtBearer(context.Configuration);
}
