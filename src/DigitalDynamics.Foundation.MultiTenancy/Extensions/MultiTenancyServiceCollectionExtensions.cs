// =============================================================================
// MultiTenancyServiceCollectionExtensions - Enregistrement des services
// =============================================================================
// Usage :
//   builder.Services.AddFoundationMultiTenancy(
//       builder.Configuration.GetSection(MultiTenancyOptions.SectionName));
//
// Ensuite dans le pipeline :
//   app.UseFoundationMultiTenancy();  // avant UseAuthorization
// =============================================================================

using DigitalDynamics.Foundation.MultiTenancy.Middleware;
using DigitalDynamics.Foundation.MultiTenancy.Pipeline;
using DigitalDynamics.Foundation.MultiTenancy.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DigitalDynamics.Foundation.MultiTenancy.Extensions;

/// <summary>
/// Extensions pour configurer les services MultiTenancy dans le conteneur DI.
/// </summary>
public static class MultiTenancyServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute les services MultiTenancy : ICurrentTenant, résolveurs, pipeline et middleware.
    /// </summary>
    /// <param name="services">Conteneur DI.</param>
    /// <param name="configuration">Section de configuration "MultiTenancy".</param>
    public static IServiceCollection AddFoundationMultiTenancy(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        services.Configure<MultiTenancyOptions>(configuration);

        services.TryAddSingleton<ICurrentTenant, CurrentTenant>();

        // Résolveurs : Header en premier (order=100), puis JWT (order=200)
        services.AddSingleton<ITenantResolver, HeaderTenantResolver>();
        services.AddSingleton<ITenantResolver, JwtClaimTenantResolver>();

        services.TryAddSingleton<TenantResolverPipeline>();

        // IMiddleware pattern : résolu par scope (par requête)
        services.AddScoped<TenantResolutionMiddleware>();

        return services;
    }
}
