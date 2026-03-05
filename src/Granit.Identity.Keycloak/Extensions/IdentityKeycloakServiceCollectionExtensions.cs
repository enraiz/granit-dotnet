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
    /// </para>
    /// <para>
    /// Minimum required role on the service account: <c>realm-management:view-users</c>.
    /// Additional roles depending on features used:
    /// <list type="bullet">
    ///   <item><description><c>realm-management:manage-users</c> — for <c>SetUserEnabledAsync</c>.</description></item>
    ///   <item><description><c>realm-management:impersonation</c> + feature <c>admin-fine-grained-authz</c> — when <c>UseTokenExchangeForDeviceActivity</c> is <c>true</c>.</description></item>
    /// </list>
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
        services.TryAddTransient<KeycloakUserTokenExchangeService>();
        services.AddIdentityProvider<KeycloakIdentityProvider>();

        return services;
    }
}
