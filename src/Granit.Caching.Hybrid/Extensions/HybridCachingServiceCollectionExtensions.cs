using Granit.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Granit.Caching.Hybrid.Extensions;

/// <summary>
/// Extensions d'enregistrement DI pour le fournisseur HybridCache.
/// </summary>
public static class HybridCachingServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre le fournisseur HybridCache (L1 mémoire locale + L2 IDistributedCache/Redis).
    /// Surcharge <see cref="ICacheService{TCacheItem}"/> par <see cref="HybridCacheService{TCacheItem}"/>.
    /// </summary>
    /// <remarks>
    /// Prérequis : <c>AddGranitCaching()</c> et <c>AddGranitCachingRedis()</c> doivent être appelés
    /// avant cette méthode (via <c>GranitCachingModule</c> et <c>GranitCachingRedisModule</c>
    /// grâce aux attributs <c>[DependsOn]</c>).
    /// </remarks>
    /// <param name="services">Collection de services.</param>
    /// <returns>La collection de services pour le chaînage.</returns>
    public static IServiceCollection AddGranitCachingHybrid(
        this IServiceCollection services)
    {
        services
            .AddOptions<HybridCachingOptions>()
            .BindConfiguration(HybridCachingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Reconfigure les options HybridCache enregistrées par GranitCachingModule
        // pour ajouter LocalCacheExpiration (L1 courte) en mode multi-pods.
        // Deferred configuration: reads CachingOptions and HybridCachingOptions at resolution time.
        services
            .AddOptions<HybridCacheOptions>()
            .Configure<IOptions<CachingOptions>, IOptions<HybridCachingOptions>>(
                (hybridCache, cachingOpts, hybridOpts) =>
                {
                    hybridCache.DefaultEntryOptions = new HybridCacheEntryOptions
                    {
                        // L2 (Redis) : expiration longue selon la config globale
                        Expiration = cachingOpts.Value.DefaultAbsoluteExpirationRelativeToNow,
                        // L1 (mémoire locale) : expiration courte pour limiter la staleness inter-pods
                        LocalCacheExpiration = hybridOpts.Value.LocalCacheExpiration,
                    };
                });

        // Surcharge ICacheService<T> avec HybridCacheService<T>
        // AddSingleton (pas TryAdd) pour remplacer l'enregistrement Memory de AddGranitCaching
        services.AddSingleton(typeof(ICacheService<>), typeof(HybridCacheService<>));

        return services;
    }
}
