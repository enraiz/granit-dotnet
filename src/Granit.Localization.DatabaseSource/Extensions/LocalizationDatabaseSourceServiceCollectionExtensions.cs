using Granit.Localization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Localization.DatabaseSource.Extensions;

/// <summary>
/// DI registration extensions for the localization DB override cache.
/// </summary>
public static class LocalizationDatabaseSourceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the caching infrastructure for localization overrides.
    /// </summary>
    /// <remarks>
    /// Registers <see cref="CachedLocalizationOverrideStore"/> as the default Singleton
    /// <see cref="ILocalizationOverrideStore"/>. The underlying raw store must be registered
    /// by a companion module (e.g. <c>GranitLocalizationDatabaseSourceEntityFrameworkCoreModule</c>)
    /// as a keyed service with key <see cref="CachedLocalizationOverrideStore.RawStoreKey"/>.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional cache TTL configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitLocalizationDatabaseSource(
        this IServiceCollection services,
        Action<LocalizationDatabaseSourceOptions>? configure = null)
    {
        services.TryAddSingleton<IMemoryCache, MemoryCache>();
        services.TryAddSingleton<ILocalizationOverrideStore, CachedLocalizationOverrideStore>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
