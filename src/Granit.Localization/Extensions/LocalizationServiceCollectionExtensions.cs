// ---------------------------------------------------------------------------
// LocalizationServiceCollectionExtensions.cs
// DI registration extension for the Granit localization system.
// Registers IStringLocalizerFactory (JsonStringLocalizerFactory) and
// IStringLocalizer<> (Microsoft's StringLocalizer<>).
// ---------------------------------------------------------------------------

using Granit.Localization.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace Granit.Localization.Extensions;

/// <summary>
/// DI registration extensions for Granit localization.
/// </summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers Granit localization services (embedded JSON).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Optional options configuration delegate.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitLocalization(
        this IServiceCollection services,
        Action<GranitLocalizationOptions>? configure = null)
    {
        services.TryAddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
        services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
