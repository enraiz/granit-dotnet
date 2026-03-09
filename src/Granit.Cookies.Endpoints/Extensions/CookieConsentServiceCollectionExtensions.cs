using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Cookies.Endpoints.Extensions;

/// <summary>
/// Extension methods for registering cookie consent endpoint services.
/// </summary>
public static class CookieConsentServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="CookieConsentConfigProvider"/> used by the
    /// <c>GET /cookies/config</c> endpoint. Call before <c>MapGranitCookieConsent()</c>.
    /// </summary>
    public static IServiceCollection AddGranitCookieConsentEndpoints(this IServiceCollection services)
    {
        services.TryAddSingleton<CookieConsentConfigProvider>();
        return services;
    }
}
