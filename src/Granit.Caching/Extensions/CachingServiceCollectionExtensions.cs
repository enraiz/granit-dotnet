using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Granit.Caching.Extensions;

/// <summary>
/// Extensions d'enregistrement DI pour <c>Granit.Caching</c>.
/// </summary>
public static class CachingServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre le système de cache Granit avec le fournisseur <c>MemoryDistributedCache</c> par défaut.
    /// </summary>
    /// <remarks>
    /// Services enregistrés :
    /// <list type="bullet">
    ///   <item><see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/> → <c>MemoryDistributedCache</c> (remplacé par les fournisseurs Redis/Hybrid si chargés)</item>
    ///   <item><see cref="IMemoryCache"/> dédié (clé <c>Granit.Caching.Locks</c>) pour la protection stampede</item>
    ///   <item><see cref="ICacheValueEncryptor"/> → <see cref="NullCacheValueEncryptor"/> (no-op par défaut)</item>
    ///   <item><see cref="ICacheService{TCacheItem}"/> → <see cref="DistributedCacheService{TCacheItem}"/></item>
    ///   <item><see cref="ICacheService{TCacheItem, TKey}"/> → <see cref="TypedKeyCacheServiceAdapter{TCacheItem, TKey}"/></item>
    /// </list>
    /// Pour activer le chiffrement AES, enregistrez <see cref="AesCacheValueEncryptor"/> après cet appel
    /// et configurez <c>Cache:Encryption:Key</c>.
    /// </remarks>
    /// <param name="services">Collection de services.</param>
    /// <param name="configuration">Section de configuration <c>"Cache"</c>. Optionnel.</param>
    /// <returns>La collection de services pour le chaînage.</returns>
    public static IServiceCollection AddGranitCaching(
        this IServiceCollection services,
        IConfigurationSection? configuration = null)
    {
        // Fournisseur Memory par défaut (remplacé par les modules Redis/Hybrid si chargés après)
        services.AddDistributedMemoryCache();

        // IMemoryCache dédié aux verrous stampede (séparé du cache applicatif)
        services.AddKeyedSingleton<IMemoryCache>(
            DistributedCacheService<object>.LockCacheKey,
            (_, _) => new MemoryCache(Options.Create(new MemoryCacheOptions
            {
                // Limite à 10 000 verrous simultanés maximum
                SizeLimit = 10_000
            })));

        // Chiffreur no-op par défaut (remplacé par AesCacheValueEncryptor si EncryptValues=true)
        services.TryAddSingleton<ICacheValueEncryptor, NullCacheValueEncryptor>();

        if (configuration is not null)
        {
            services.Configure<CachingOptions>(configuration);
            // "Cache:Encryption" → sous-section "Encryption" de la section "Cache"
            services.Configure<CacheEncryptionOptions>(configuration.GetSection("Encryption"));
        }

        services.TryAddSingleton(typeof(ICacheService<>), typeof(DistributedCacheService<>));
        services.TryAddSingleton(typeof(ICacheService<,>), typeof(TypedKeyCacheServiceAdapter<,>));

        // HybridCache memory-only par défaut (L1 uniquement, pas de L2)
        // GranitCachingHybridModule reconfigure les options pour ajouter L2 Redis + LocalCacheExpiration
        CachingOptions cachingOpts = new();
        configuration?.Bind(cachingOpts);

        services.AddHybridCache(hybrid =>
        {
            hybrid.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = cachingOpts.DefaultAbsoluteExpirationRelativeToNow,
            };
        });

        return services;
    }
}
