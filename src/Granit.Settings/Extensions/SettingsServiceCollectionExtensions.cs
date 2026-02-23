using Granit.Settings.Definitions;
using Granit.Settings.Options;
using Granit.Settings.Providers;
using Granit.Settings.Services;
using Granit.Settings.Stores;
using Granit.Settings.Values;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Settings.Extensions;

/// <summary>
/// Extensions for configuring Granit.Settings services in the DI container.
/// </summary>
public static class SettingsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Settings module services: definitions, store, providers and services.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configuration">Configuration section "Settings". Optional.</param>
    public static IServiceCollection AddGranitSettings(
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
