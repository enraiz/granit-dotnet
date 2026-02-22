// =============================================================================
// SettingsServiceCollectionExtensions - DI registration for the Settings module
// =============================================================================
// Usage:
//   builder.Services.AddFoundationSettings(
//       builder.Configuration.GetSection(SettingsOptions.SectionName));
//
// Registered services:
//   - SettingDefinitionManager       (Singleton)
//   - ISettingStore                  → InMemorySettingStore (Singleton, replaceable)
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
/// Extensions for configuring Foundation.Settings services in the DI container.
/// </summary>
public static class SettingsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Settings module services: definitions, store, providers and services.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configuration">Configuration section "Settings". Optional.</param>
    public static IServiceCollection AddFoundationSettings(
        this IServiceCollection services,
        IConfigurationSection? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<SettingsOptions>(configuration);
        }

        // Definition registry (Singleton — loaded once at startup)
        services.TryAddSingleton<SettingDefinitionManager>();

        // Default in-memory store (replaced by EfCoreSettingStore in production)
        services.TryAddSingleton<ISettingStore, InMemorySettingStore>();

        // Providers (Singleton — no per-request state)
        services.AddSingleton<ISettingValueProvider, UserSettingValueProvider>();
        services.AddSingleton<ISettingValueProvider, TenantSettingValueProvider>();
        services.AddSingleton<ISettingValueProvider, GlobalSettingValueProvider>();
        services.AddSingleton<ISettingValueProvider, ConfigurationSettingValueProvider>();
        services.AddSingleton<ISettingValueProvider, DefaultValueSettingValueProvider>();

        services.TryAddSingleton<SettingValueProviderManager>();

        // Application services (Scoped — tenant/user context per request)
        services.TryAddScoped<ISettingProvider, SettingProvider>();
        services.TryAddScoped<ISettingManager, SettingManager>();

        return services;
    }
}
