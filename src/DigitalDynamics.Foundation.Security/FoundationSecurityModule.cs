// =============================================================================
// FoundationSecurityModule - Module Foundation pour l'authentification Keycloak
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Security.Extensions;

namespace DigitalDynamics.Foundation.Security;

/// <summary>
/// Module Foundation pour Keycloak authentication et ICurrentUserService.
/// Aucune dependance sur d'autres modules Foundation.
/// </summary>
public sealed class FoundationSecurityModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationSecurity(context.Configuration);
}
