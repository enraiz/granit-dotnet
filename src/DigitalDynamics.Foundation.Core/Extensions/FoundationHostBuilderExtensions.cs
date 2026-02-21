// =============================================================================
// FoundationHostBuilderExtensions - Point d'entree du systeme de modules
// =============================================================================
// Extensions builder.AddFoundation<TModule>() (sync) et
// builder.AddFoundationAsync<TModule>() (async) qui decouvrent, trient et
// configurent tous les modules Foundation en un seul appel.
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DigitalDynamics.Foundation.Core.Extensions;

/// <summary>
/// Extensions sur <see cref="IHostApplicationBuilder"/> pour enregistrer
/// le systeme de modules Foundation.
/// </summary>
public static class FoundationHostBuilderExtensions
{
    /// <summary>
    /// Decouvre et configure tous les modules Foundation a partir du module racine
    /// <typeparamref name="TModule"/> (version synchrone).
    /// Les modules sont charges dans l'ordre topologique (dependances d'abord)
    /// et leurs <see cref="FoundationModule.ConfigureServices"/> sont appeles.
    /// </summary>
    /// <typeparam name="TModule">Module racine de l'application.</typeparam>
    public static IHostApplicationBuilder AddFoundation<TModule>(
        this IHostApplicationBuilder builder)
        where TModule : FoundationModule
    {
        var modules = ModuleLoader.LoadModules<TModule>();
        var application = new FoundationApplication(modules);

        var context = new ServiceConfigurationContext(
            builder.Services,
            builder.Configuration,
            builder);

        application.ConfigureServices(context);

        builder.Services.AddSingleton(application);

        return builder;
    }

    /// <summary>
    /// Decouvre et configure tous les modules Foundation a partir du module racine
    /// <typeparamref name="TModule"/> (version asynchrone).
    /// Les modules sont charges dans l'ordre topologique (dependances d'abord)
    /// et leurs <see cref="FoundationModule.ConfigureServicesAsync"/> sont appeles.
    /// Preferer cette methode quand des modules necessitent une initialisation async.
    /// </summary>
    /// <typeparam name="TModule">Module racine de l'application.</typeparam>
    public static async Task<IHostApplicationBuilder> AddFoundationAsync<TModule>(
        this IHostApplicationBuilder builder)
        where TModule : FoundationModule
    {
        var modules = ModuleLoader.LoadModules<TModule>();
        var application = new FoundationApplication(modules);

        var context = new ServiceConfigurationContext(
            builder.Services,
            builder.Configuration,
            builder);

        await application.ConfigureServicesAsync(context);

        builder.Services.AddSingleton(application);

        return builder;
    }
}
