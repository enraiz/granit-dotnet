// ---------------------------------------------------------------------------
// LocalizationServiceCollectionExtensions.cs
// Extension d'enregistrement DI pour le système de localisation Foundation.
// Enregistre IStringLocalizerFactory (JsonStringLocalizerFactory) et
// IStringLocalizer<> (StringLocalizer<> de Microsoft).
// ---------------------------------------------------------------------------

using DigitalDynamics.Foundation.Localization.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace DigitalDynamics.Foundation.Localization.Extensions;

/// <summary>
/// Extensions d'enregistrement DI pour la localisation Foundation.
/// </summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre les services de localisation Foundation (JSON embarqué).
    /// </summary>
    /// <param name="services">Collection de services.</param>
    /// <param name="configure">Configuration optionnelle des options.</param>
    /// <returns>La collection de services pour chaînage.</returns>
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
