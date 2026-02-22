using DigitalDynamics.Foundation.Core.Modularity;

namespace DigitalDynamics.Foundation.Security;

/// <summary>
/// Foundation module providing security abstractions (<see cref="ICurrentUserService"/>).
/// No services are registered here — use <c>FoundationJwtBearerModule</c> or
/// <c>FoundationAuthenticationKeycloakModule</c> for the full implementation.
/// </summary>
public sealed class FoundationSecurityModule : FoundationModule;
