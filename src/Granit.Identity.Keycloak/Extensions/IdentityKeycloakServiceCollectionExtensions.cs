using Granit.Identity.Extensions;
using Granit.Identity.Keycloak.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Identity.Keycloak.Extensions;

/// <summary>
/// Extension methods for registering the Keycloak identity provider.
/// </summary>
public static class IdentityKeycloakServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Keycloak Admin API as the <see cref="IIdentityProvider"/> implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires the <c>KeycloakAdmin</c> configuration section with:
    /// <c>BaseUrl</c>, <c>Realm</c>, <c>ClientId</c>, <c>ClientSecret</c>.
    /// The service account must have the <c>realm-management:view-users</c> role.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitIdentityKeycloak(
        this IServiceCollection services)
    {
        services.AddOptions<KeycloakAdminOptions>()
            .BindConfiguration(KeycloakAdminOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient("KeycloakAdmin");
        services.TryAddSingleton<KeycloakAdminTokenService>();
        services.AddIdentityProvider<KeycloakIdentityProvider>();

        return services;
    }
}
