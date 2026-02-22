// =============================================================================
// FoundationSecurityModule - Security abstractions module
// =============================================================================
// Base module providing ICurrentUserService (interface only).
// The implementation and JWT Bearer configuration are in:
//   Foundation.Authentication.JwtBearer  (FoundationJwtBearerModule)
//   Foundation.Authentication.Keycloak   (FoundationAuthenticationKeycloakModule)
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;

namespace DigitalDynamics.Foundation.Security;

/// <summary>
/// Foundation module providing security abstractions (<see cref="ICurrentUserService"/>).
/// No services are registered here — use <c>FoundationJwtBearerModule</c> or
/// <c>FoundationAuthenticationKeycloakModule</c> for the full implementation.
/// </summary>
<<<<<<< HEAD
public sealed class FoundationSecurityModule : FoundationModule;
=======
public sealed class FoundationSecurityModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationSecurity(context.Configuration);
}
>>>>>>> feature/settings-module
