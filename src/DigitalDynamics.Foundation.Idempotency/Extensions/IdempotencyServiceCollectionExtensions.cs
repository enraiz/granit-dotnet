using DigitalDynamics.Foundation.Idempotency.Abstractions;
using DigitalDynamics.Foundation.Idempotency.Internal;
using DigitalDynamics.Foundation.Idempotency.Models;
using DigitalDynamics.Foundation.Idempotency.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace DigitalDynamics.Foundation.Idempotency.Extensions;

/// <summary>
/// Extension methods for registering Foundation Idempotency services.
/// </summary>
public static class IdempotencyServiceCollectionExtensions
{
    /// <summary>
    /// Registers Foundation Idempotency services using a configuration section.
    /// </summary>
    public static IServiceCollection AddFoundationIdempotency(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        services.Configure<IdempotencyOptions>(configurationSection);
        return services.AddFoundationIdempotencyCore();
    }

    /// <summary>
    /// Registers Foundation Idempotency services with an options delegate.
    /// </summary>
    public static IServiceCollection AddFoundationIdempotency(
        this IServiceCollection services,
        Action<IdempotencyOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services.AddFoundationIdempotencyCore();
    }

    private static IServiceCollection AddFoundationIdempotencyCore(this IServiceCollection services)
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
