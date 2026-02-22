using DigitalDynamics.Foundation.Caching;
using DigitalDynamics.Foundation.Caching.StackExchangeRedis.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

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

    /// <summary>
    /// Adds a Redis connectivity health check tagged <c>"readiness"</c>.
    /// Issues a PING command and measures round-trip latency. Returns <c>Degraded</c>
    /// when latency exceeds <paramref name="degradedThreshold"/> and <c>Unhealthy</c>
    /// when Redis is unreachable.
    /// </summary>
    /// <remarks>
    /// Registers <see cref="IConnectionMultiplexer"/> as a singleton if not already registered.
    /// Call <see cref="AddFoundationCachingRedis"/> before this method so that
    /// <see cref="RedisCachingOptions"/> is configured.
    /// </remarks>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">Check name. Defaults to <c>"redis"</c>.</param>
    /// <param name="degradedThreshold">Latency above which the check returns Degraded. Defaults to 100 ms.</param>
    /// <param name="failureStatus">Status on failure. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="timeout">Check timeout. Defaults to 5 seconds.</param>
    public static IHealthChecksBuilder AddFoundationRedisCheck(
        this IHealthChecksBuilder builder,
        string name = "redis",
        TimeSpan? degradedThreshold = null,
        HealthStatus? failureStatus = null,
        TimeSpan? timeout = null)
    {
        TimeSpan threshold = degradedThreshold ?? TimeSpan.FromMilliseconds(100);

        if (builder.Services.All(d => d.ServiceType != typeof(IConnectionMultiplexer)))
        {
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                RedisCachingOptions opts = sp.GetRequiredService<IOptions<RedisCachingOptions>>().Value;
                return ConnectionMultiplexer.Connect(opts.Configuration);
            });
        }

        builder.Services.AddSingleton(sp =>
            new RedisHealthCheck(sp.GetRequiredService<IConnectionMultiplexer>(), threshold));

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => sp.GetRequiredService<RedisHealthCheck>(),
            failureStatus,
            ["readiness"],
            timeout ?? TimeSpan.FromSeconds(5)));
    }
}
