// =============================================================================
// FoundationJwtBearerModule - Generic JWT Bearer module
// =============================================================================
// Configures generic OIDC-compatible JWT Bearer authentication and
// registers ICurrentUserService via HttpContext.
// Reads the "Authentication" section of appsettings.json.
//
// For Keycloak: use FoundationAuthenticationKeycloakModule
// (Foundation.Authentication.Keycloak) which depends on this module.
// =============================================================================

using DigitalDynamics.Foundation.Authentication.JwtBearer.Extensions;
using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Security;

namespace DigitalDynamics.Foundation.Authentication.JwtBearer;

/// <summary>
/// Foundation module for generic OIDC JWT Bearer authentication.
/// Depends on <see cref="FoundationSecurityModule"/> for the abstractions.
/// </summary>
[DependsOn(typeof(FoundationSecurityModule))]
public sealed class FoundationJwtBearerModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationJwtBearer(context.Configuration);
}
