using Granit.Cookies.Klaro.Internal;
using Granit.Cookies.Klaro.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Cookies.Klaro.Extensions;

/// <summary>
/// Extension methods for registering the Klaro CMP integration.
/// </summary>
public static class KlaroServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Klaro consent resolver and binds <see cref="KlaroOptions"/>
    /// from the <c>Klaro</c> configuration section.
    /// </summary>
    public static IServiceCollection AddGranitCookiesKlaro(this IServiceCollection services)
    {
        services.AddOptions<KlaroOptions>()
            .BindConfiguration(KlaroOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IConsentResolver, KlaroConsentResolver>();

        return services;
    }
}
