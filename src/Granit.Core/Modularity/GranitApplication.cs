namespace Granit.Core.Modularity;

/// <summary>
/// Orchestrates the lifecycle of Granit modules.
/// Registered as a singleton by <c>AddGranit&lt;T&gt;()</c>
/// or <c>AddGranitAsync&lt;T&gt;()</c>.
/// </summary>
public sealed class GranitApplication
{
    private readonly IReadOnlyList<ModuleDescriptor> _modules;

    internal GranitApplication(IReadOnlyList<ModuleDescriptor> modules)
    {
        _modules = modules;
    }

    /// <summary>Returns the types of loaded modules in topological order (for diagnostics).</summary>
    public IReadOnlyList<Type> GetModuleTypes() =>
        [.. _modules.Select(m => m.ModuleType)];

    /// <summary>
    /// Calls <see cref="GranitModule.ConfigureServices"/> on each module
    /// in topological order (synchronous version).
    /// </summary>
    internal void ConfigureServices(ServiceConfigurationContext context)
    {
        foreach (ModuleDescriptor module in _modules)
        {
            module.Instance.ConfigureServices(context);
        }
    }

    /// <summary>
    /// Calls <see cref="GranitModule.ConfigureServicesAsync"/> on each module
    /// in topological order (asynchronous version).
    /// </summary>
    internal async Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        foreach (ModuleDescriptor module in _modules)
        {
            await module.Instance.ConfigureServicesAsync(context).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Calls <see cref="GranitModule.OnApplicationInitialization"/> on each module
    /// in topological order (synchronous version).
    /// </summary>
    internal void InitializeApplication(ApplicationInitializationContext context)
    {
        foreach (ModuleDescriptor module in _modules)
        {
            module.Instance.OnApplicationInitialization(context);
        }
    }

    /// <summary>
    /// Calls <see cref="GranitModule.OnApplicationInitializationAsync"/> on each module
    /// in topological order (asynchronous version).
    /// </summary>
    internal async Task InitializeApplicationAsync(ApplicationInitializationContext context)
    {
        foreach (ModuleDescriptor module in _modules)
        {
            await module.Instance.OnApplicationInitializationAsync(context).ConfigureAwait(false);
        }
    }
}
