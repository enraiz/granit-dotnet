using DigitalDynamics.Foundation.MultiTenancy.Middleware;
using DigitalDynamics.Foundation.MultiTenancy.Pipeline;
using DigitalDynamics.Foundation.MultiTenancy.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DigitalDynamics.Foundation.MultiTenancy.Extensions;

/// <summary>
/// Extensions for configuring MultiTenancy services in the DI container.
/// </summary>
public static class MultiTenancyServiceCollectionExtensions
{
    /// <summary>
    /// Adds MultiTenancy services: ICurrentTenant, resolvers, pipeline, and middleware.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configuration">Configuration section "MultiTenancy".</param>
    public static IServiceCollection AddFoundationMultiTenancy(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        services.Configure<MultiTenancyOptions>(configuration);

        services.TryAddSingleton<ICurrentTenant, CurrentTenant>();

        // Resolvers: Header first (order=100), then JWT (order=200)
        services.AddSingleton<ITenantResolver, HeaderTenantResolver>();
        services.AddSingleton<ITenantResolver, JwtClaimTenantResolver>();

        services.TryAddSingleton<TenantResolverPipeline>();

        // IMiddleware pattern: resolved per scope (per request)
        services.AddScoped<TenantResolutionMiddleware>();

        return services;
    }
}
