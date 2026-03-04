using Granit.Core.Modularity;
using Granit.Identity.Keycloak.Extensions;

namespace Granit.Identity.Keycloak;

/// <summary>
/// Granit module that registers the Keycloak Admin API as the
/// <see cref="IIdentityProvider"/> implementation.
/// </summary>
[DependsOn(typeof(GranitIdentityModule))]
public sealed class GranitIdentityKeycloakModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitIdentityKeycloak();
}
