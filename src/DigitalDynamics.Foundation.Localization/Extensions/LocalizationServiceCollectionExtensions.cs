// ---------------------------------------------------------------------------
// LocalizationServiceCollectionExtensions.cs
// DI registration extension for the Foundation localization system.
// Registers IStringLocalizerFactory (JsonStringLocalizerFactory) and
// IStringLocalizer<> (Microsoft's StringLocalizer<>).
// ---------------------------------------------------------------------------

using DigitalDynamics.Foundation.Localization.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace DigitalDynamics.Foundation.Localization.Extensions;

/// <summary>
/// DI registration extensions for Foundation localization.
/// </summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers Foundation localization services (embedded JSON).
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Optional options configuration delegate.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFoundationLocalization(
        this IServiceCollection services,
        Action<FoundationLocalizationOptions>? configure = null)
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
