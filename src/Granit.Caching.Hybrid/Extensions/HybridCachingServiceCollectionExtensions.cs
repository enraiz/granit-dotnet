using Granit.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    /// <param name="configuration">Configuration racine (non la section "Cache").</param>
    /// <returns>La collection de services pour le chaînage.</returns>
    public static IServiceCollection AddGranitCachingHybrid(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        HybridCachingOptions hybridOpts = configuration
            .GetSection(HybridCachingOptions.SectionName)
            .Get<HybridCachingOptions>() ?? new HybridCachingOptions();

        services.Configure<HybridCachingOptions>(
            configuration.GetSection(HybridCachingOptions.SectionName));

        CachingOptions cachingOpts = configuration
            .GetSection(CachingOptions.SectionName)
            .Get<CachingOptions>() ?? new CachingOptions();

        services.AddHybridCache(hybridCache =>
        {
            hybridCache.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                // L2 (Redis) : expiration longue selon la config globale
                Expiration = cachingOpts.DefaultAbsoluteExpirationRelativeToNow,
                // L1 (mémoire locale) : expiration courte pour limiter la staleness inter-pods
                LocalCacheExpiration = hybridOpts.LocalCacheExpiration,
            };
        });

        // Surcharge ICacheService<T> avec HybridCacheService<T>
        // AddSingleton (pas TryAdd) pour remplacer l'enregistrement Memory de AddGranitCaching
        services.AddSingleton(typeof(ICacheService<>), typeof(HybridCacheService<>));

        return services;
    }
}
