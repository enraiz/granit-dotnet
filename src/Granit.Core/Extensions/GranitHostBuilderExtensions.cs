using Granit.Core.Modularity;
using Granit.Core.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Granit.Core.Extensions;

/// <summary>
/// Extensions sur <see cref="IHostApplicationBuilder"/> pour enregistrer
/// le systeme de modules Granit.
/// </summary>
public static class GranitHostBuilderExtensions
{
    /// <summary>
    /// Decouvre et configure tous les modules Granit a partir du module racine
    /// <typeparamref name="TModule"/> (version synchrone).
    /// Les modules sont charges dans l'ordre topologique (dependances d'abord)
    /// et leurs <see cref="GranitModule.ConfigureServices"/> sont appeles.
    /// </summary>
    /// <typeparam name="TModule">Module racine de l'application.</typeparam>
    public static IHostApplicationBuilder AddGranit<TModule>(
        this IHostApplicationBuilder builder)
        where TModule : GranitModule
    {
        IReadOnlyList<ModuleDescriptor> modules = ModuleLoader.LoadModules<TModule>();
        GranitApplication application = new(modules);

        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Default: NullTenantContext (no-op). Granit.MultiTenancy replaces it if present.
        builder.Services.TryAddSingleton<ICurrentTenant>(NullTenantContext.Instance);

        application.ConfigureServices(context);

        builder.Services.AddSingleton(application);

        return builder;
    }

    /// <summary>
    /// Decouvre et configure tous les modules Granit a partir du module racine
    /// <typeparamref name="TModule"/> (version asynchrone).
    /// Les modules sont charges dans l'ordre topologique (dependances d'abord)
    /// et leurs <see cref="GranitModule.ConfigureServicesAsync"/> sont appeles.
    /// Preferer cette methode quand des modules necessitent une initialisation async.
    /// </summary>
    /// <typeparam name="TModule">Module racine de l'application.</typeparam>
    public static async Task<IHostApplicationBuilder> AddGranitAsync<TModule>(
        this IHostApplicationBuilder builder)
        where TModule : GranitModule
    {
        IReadOnlyList<ModuleDescriptor> modules = ModuleLoader.LoadModules<TModule>();
        GranitApplication application = new(modules);

        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Default: NullTenantContext (no-op). Granit.MultiTenancy replaces it if present.
        builder.Services.TryAddSingleton<ICurrentTenant>(NullTenantContext.Instance);

        await application.ConfigureServicesAsync(context);

        builder.Services.AddSingleton(application);

        return builder;
    }
}
