using System.Reflection;
using Granit.Core.Modularity;
using Granit.Core.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        ILogger<GranitApplication> logger = CreateBootstrapLogger(builder);
        GranitApplication application = new(modules, logger);
        IReadOnlyList<Assembly> moduleAssemblies = GetDistinctModuleAssemblies(modules);

        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder,
            moduleAssemblies);

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
        ILogger<GranitApplication> logger = CreateBootstrapLogger(builder);
        GranitApplication application = new(modules, logger);
        IReadOnlyList<Assembly> moduleAssemblies = GetDistinctModuleAssemblies(modules);

        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder,
            moduleAssemblies);

        // Default: NullTenantContext (no-op). Granit.MultiTenancy replaces it if present.
        builder.Services.TryAddSingleton<ICurrentTenant>(NullTenantContext.Instance);

        await application.ConfigureServicesAsync(context).ConfigureAwait(false);

        builder.Services.AddSingleton(application);

        return builder;
    }

    private static IReadOnlyList<Assembly> GetDistinctModuleAssemblies(
        IReadOnlyList<ModuleDescriptor> modules) =>
        [.. modules.Select(m => m.ModuleType.Assembly).Distinct()];

    /// <summary>
    /// Creates a bootstrap logger from the host builder's logging configuration.
    /// This logger is available before the full DI container is built.
    /// </summary>
    private static ILogger<GranitApplication> CreateBootstrapLogger(
        IHostApplicationBuilder builder)
    {
        using ILoggerFactory factory = LoggerFactory.Create(lb =>
            lb.AddConfiguration(builder.Configuration.GetSection("Logging")));
        return factory.CreateLogger<GranitApplication>();
    }
}
