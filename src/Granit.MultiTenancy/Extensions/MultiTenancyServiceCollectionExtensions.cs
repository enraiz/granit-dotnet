using Granit.MultiTenancy.Middleware;
using Granit.MultiTenancy.Pipeline;
using Granit.MultiTenancy.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.MultiTenancy.Extensions;

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
    public static IServiceCollection AddGranitMultiTenancy(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        services.Configure<MultiTenancyOptions>(configuration);

        // Replace the NullTenantContext registered by AddGranit<T>() with the real implementation.
        services.Replace(ServiceDescriptor.Singleton<ICurrentTenant, CurrentTenant>());

        // Resolvers: Header first (order=100), then JWT (order=200)
        services.AddSingleton<ITenantResolver, HeaderTenantResolver>();
        services.AddSingleton<ITenantResolver, JwtClaimTenantResolver>();

        services.TryAddSingleton<TenantResolverPipeline>();

        // IMiddleware pattern: resolved per scope (per request)
        services.AddScoped<TenantResolutionMiddleware>();

        return services;
    }
}
