using Granit.Authentication.JwtBearer.Extensions;
using Microsoft.AspNetCore.Routing;

namespace Granit.Authentication.Keycloak.Extensions;

/// <summary>
/// Extensions for mapping Keycloak-related endpoints on <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class KeycloakEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the OIDC back-channel logout endpoint when back-channel logout is enabled.
    /// Delegates to <see cref="JwtBearerEndpointRouteBuilderExtensions.MapBackChannelLogout"/>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The <paramref name="endpoints"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapKeycloakBackChannelLogout(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapBackChannelLogout();
}
