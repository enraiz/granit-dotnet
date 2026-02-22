namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Orchestre le cycle de vie des modules Foundation.
/// Enregistre comme singleton par <c>AddFoundation&lt;T&gt;()</c>
/// ou <c>AddFoundationAsync&lt;T&gt;()</c>.
/// </summary>
public sealed class FoundationApplication
{
    private readonly IReadOnlyList<ModuleDescriptor> _modules;

    internal FoundationApplication(IReadOnlyList<ModuleDescriptor> modules)
    {
        _modules = modules;
    }

    /// <summary>Retourne les types des modules chargés en ordre topologique (pour diagnostics).</summary>
    public IReadOnlyList<Type> GetModuleTypes() =>
        _modules.Select(m => m.ModuleType).ToList();

    /// <summary>
    /// Appelle <see cref="FoundationModule.ConfigureServices"/> sur chaque module
    /// dans l'ordre topologique (version synchrone).
    /// </summary>
    internal void ConfigureServices(ServiceConfigurationContext context)
    {
        foreach (var module in _modules)
        {
            module.Instance.ConfigureServices(context);
        }
    }

    /// <summary>
    /// Appelle <see cref="FoundationModule.ConfigureServicesAsync"/> sur chaque module
    /// dans l'ordre topologique (version asynchrone).
    /// </summary>
    internal async Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        foreach (var module in _modules)
        {
            await module.Instance.ConfigureServicesAsync(context);
        }
    }

    /// <summary>
    /// Appelle <see cref="FoundationModule.OnApplicationInitialization"/> sur chaque module
    /// dans l'ordre topologique (version synchrone).
    /// </summary>
    internal void InitializeApplication(ApplicationInitializationContext context)
    {
        foreach (var module in _modules)
        {
            module.Instance.OnApplicationInitialization(context);
        }
    }

    /// <summary>
    /// Appelle <see cref="FoundationModule.OnApplicationInitializationAsync"/> sur chaque module
    /// dans l'ordre topologique (version asynchrone).
    /// </summary>
    internal async Task InitializeApplicationAsync(ApplicationInitializationContext context)
    {
        foreach (var module in _modules)
        {
            await module.Instance.OnApplicationInitializationAsync(context);
        }
    }
}
