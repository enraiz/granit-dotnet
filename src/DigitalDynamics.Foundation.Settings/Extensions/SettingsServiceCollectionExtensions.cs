// =============================================================================
// SettingsServiceCollectionExtensions - Enregistrement DI du module Settings
// =============================================================================
// Usage :
//   builder.Services.AddFoundationSettings(
//       builder.Configuration.GetSection(SettingsOptions.SectionName));
//
// Services enregistrés :
//   - SettingDefinitionManager       (Singleton)
//   - ISettingStore                  → InMemorySettingStore (Singleton, remplaçable)
//   - ISettingValueProvider[D,C,G,T,U] (Singleton)
//   - SettingValueProviderManager    (Singleton)
//   - ISettingProvider               → SettingProvider (Scoped)
//   - ISettingManager                → SettingManager  (Scoped)
// =============================================================================

using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Options;
using DigitalDynamics.Foundation.Settings.Providers;
using DigitalDynamics.Foundation.Settings.Services;
using DigitalDynamics.Foundation.Settings.Stores;
using DigitalDynamics.Foundation.Settings.Values;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DigitalDynamics.Foundation.Settings.Extensions;

/// <summary>
/// Extensions pour configurer les services Foundation.Settings dans le conteneur DI.
/// </summary>
public static class SettingsServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute les services du module Settings : définitions, store, providers et services.
    /// </summary>
    /// <param name="services">Conteneur DI.</param>
    /// <param name="configuration">Section de configuration "Settings". Optionnel.</param>
    public static IServiceCollection AddFoundationSettings(
        this IServiceCollection services,
        IConfigurationSection? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<SettingsOptions>(configuration);
        }

        // Registre des définitions (Singleton — chargé une seule fois au démarrage)
        services.TryAddSingleton<SettingDefinitionManager>();

        // Store en mémoire par défaut (remplacé par EfCoreSettingStore en production)
        services.TryAddSingleton<ISettingStore, InMemorySettingStore>();

        // Providers (Singleton — pas d'état par requête)
        services.AddSingleton<ISettingValueProvider, UserSettingValueProvider>();
        services.AddSingleton<ISettingValueProvider, TenantSettingValueProvider>();
        services.AddSingleton<ISettingValueProvider, GlobalSettingValueProvider>();
        services.AddSingleton<ISettingValueProvider, ConfigurationSettingValueProvider>();
        services.AddSingleton<ISettingValueProvider, DefaultValueSettingValueProvider>();

        services.TryAddSingleton<SettingValueProviderManager>();

        // Services applicatifs (Scoped — contexte tenant/user par requête)
        services.TryAddScoped<ISettingProvider, SettingProvider>();
        services.TryAddScoped<ISettingManager, SettingManager>();

        return services;
    }
}
