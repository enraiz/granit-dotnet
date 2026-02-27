using Granit.Cookies.Internal;
using Granit.Cookies.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Cookies.Extensions;

/// <summary>
/// Extensions for registering Granit.Cookies module services.
/// </summary>
public static class CookiesServiceCollectionExtensions
{
    /// <summary>
    /// Adds Granit.Cookies services (ICookieRegistry, IGranitCookieManager)
    /// and registers cookie definitions declared in the builder.
    /// </summary>
    public static IServiceCollection AddGranitCookies(
        this IServiceCollection services,
        Action<GranitCookiesBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<GranitCookiesOptions>()
            .BindConfiguration(GranitCookiesOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        CookieRegistry registry = new();
        GranitCookiesBuilder builder = new(services);
        configure(builder);

        foreach (CookieDefinition definition in builder.CookieDefinitions)
        {
            registry.Register(definition);
        }

        services.TryAddSingleton<ICookieRegistry>(registry);
        services.TryAddScoped<IGranitCookieManager, GranitCookieManager>();

        return services;
    }
}
