// =============================================================================
// FoundationSecurityModule - Module Foundation pour l'authentification JWT Bearer générique
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Security.Extensions;

namespace DigitalDynamics.Foundation.Security;

/// <summary>
/// Module Foundation pour l'authentification JWT Bearer générique (OIDC) et ICurrentUserService.
/// Lit la section <c>"Authentication"</c> de la configuration.
/// Pour Keycloak : utiliser <see cref="FoundationSecurityKeycloakModule"/> (Foundation.Security.Keycloak).
/// Aucune dépendance sur d'autres modules Foundation.
/// </summary>
public sealed class FoundationSecurityModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddFoundationSecurity(context.Configuration);
    }
}
