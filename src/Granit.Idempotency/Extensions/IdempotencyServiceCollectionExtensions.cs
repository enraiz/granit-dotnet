using Granit.Idempotency.Abstractions;
using Granit.Idempotency.Internal;
using Granit.Idempotency.Models;
using Granit.Idempotency.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace Granit.Idempotency.Extensions;

/// <summary>
/// Extension methods for registering Granit Idempotency services.
/// </summary>
public static class IdempotencyServiceCollectionExtensions
{
    /// <summary>
    /// Registers Granit Idempotency services using a configuration section.
    /// </summary>
    public static IServiceCollection AddGranitIdempotency(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        services.Configure<IdempotencyOptions>(configurationSection);
        return services.AddGranitIdempotencyCore();
    }

    /// <summary>
    /// Registers Granit Idempotency services with an options delegate.
    /// </summary>
    public static IServiceCollection AddGranitIdempotency(
        this IServiceCollection services,
        Action<IdempotencyOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services.AddGranitIdempotencyCore();
    }

    private static IServiceCollection AddGranitIdempotencyCore(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<IdempotencyOptions>, IdempotencyOptionsValidator>();
        services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();

        // RecyclableMemoryStreamManager is thread-safe and should be a singleton
        services.TryAddSingleton<RecyclableMemoryStreamManager>();

        // IMiddleware pattern: AddTransient so scoped services (ICurrentUserService, ICurrentTenant)
        // are resolved from the request scope via IMiddlewareFactory.
        services.AddTransient<IdempotencyMiddleware>();

        return services;
    }
}
