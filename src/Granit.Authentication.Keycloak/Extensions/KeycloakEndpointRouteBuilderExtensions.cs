using Granit.Authentication.Keycloak.BackChannelLogout;
using Granit.Authentication.Keycloak.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Granit.Authentication.Keycloak.Extensions;

/// <summary>
/// Extensions for mapping Keycloak-related endpoints on <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class KeycloakEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the Keycloak back-channel logout endpoint when
    /// <see cref="BackChannelLogoutOptions.Enabled"/> is <c>true</c>.
    /// The endpoint accepts anonymous POST requests (Keycloak calls it server-to-server).
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The <paramref name="endpoints"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapKeycloakBackChannelLogout(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<KeycloakOptions>>().Value;

        if (!options.BackChannelLogout.Enabled)
        {
            return endpoints;
        }

        endpoints
            .MapPost(options.BackChannelLogout.EndpointPath, BackChannelLogoutEndpoint.HandleAsync)
            .AllowAnonymous()
            .ExcludeFromDescription();

        return endpoints;
    }
}
