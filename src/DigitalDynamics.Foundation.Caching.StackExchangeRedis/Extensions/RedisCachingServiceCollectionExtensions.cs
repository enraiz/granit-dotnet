using DigitalDynamics.Foundation.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Caching.StackExchangeRedis.Extensions;

/// <summary>
/// Extensions d'enregistrement DI pour le fournisseur Redis.
/// </summary>
public static class RedisCachingServiceCollectionExtensions
{
    /// <summary>
    /// Remplace le fournisseur Memory par Redis comme <c>IDistributedCache</c>.
    /// Active le chiffrement AES-256 (<see cref="AesCacheValueEncryptor"/>) si
    /// <c>CachingOptions.EncryptValues = true</c>.
    /// </summary>
    /// <remarks>
    /// Si <see cref="RedisCachingOptions.IsEnabled"/> est <c>false</c>, cette méthode est sans effet
    /// et le fournisseur Memory enregistré par <c>AddFoundationCaching()</c> reste actif.
    /// </remarks>
    /// <param name="services">Collection de services.</param>
    /// <param name="configuration">Configuration racine (non la section "Cache").</param>
    /// <returns>La collection de services pour le chaînage.</returns>
    public static IServiceCollection AddFoundationCachingRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        RedisCachingOptions redisOpts = configuration
            .GetSection(RedisCachingOptions.SectionName)
            .Get<RedisCachingOptions>() ?? new RedisCachingOptions();

        if (!redisOpts.IsEnabled)
        {
            return services;
        }

        services.Configure<RedisCachingOptions>(
            configuration.GetSection(RedisCachingOptions.SectionName));

        // Remplace IDistributedCache (MemoryDistributedCache → RedisCache)
        services.AddStackExchangeRedisCache(redis =>
        {
            redis.Configuration = redisOpts.Configuration;
            redis.InstanceName = redisOpts.InstanceName;
        });

        // Active le chiffrement AES-256 si demandé globalement
        CachingOptions cachingOpts = configuration
            .GetSection(CachingOptions.SectionName)
            .Get<CachingOptions>() ?? new CachingOptions();

        if (cachingOpts.EncryptValues)
        {
            services.Configure<CacheEncryptionOptions>(
                configuration.GetSection(CacheEncryptionOptions.SectionName));

            // Remplace NullCacheValueEncryptor par AesCacheValueEncryptor
            services.AddSingleton<ICacheValueEncryptor, AesCacheValueEncryptor>();
        }

        return services;
    }
}
