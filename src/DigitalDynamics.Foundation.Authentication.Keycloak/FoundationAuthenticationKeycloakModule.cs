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
