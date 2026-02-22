namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Orchestrates the lifecycle of Foundation modules.
/// Registered as a singleton by <c>AddFoundation&lt;T&gt;()</c>
/// or <c>AddFoundationAsync&lt;T&gt;()</c>.
/// </summary>
public sealed class FoundationApplication
{
    private readonly IReadOnlyList<ModuleDescriptor> _modules;

    internal FoundationApplication(IReadOnlyList<ModuleDescriptor> modules)
    {
        _modules = modules;
    }

    /// <summary>Returns the types of loaded modules in topological order (for diagnostics).</summary>
    public IReadOnlyList<Type> GetModuleTypes() =>
        _modules.Select(m => m.ModuleType).ToList();

    /// <summary>
    /// Calls <see cref="FoundationModule.ConfigureServices"/> on each module
    /// in topological order (synchronous version).
    /// </summary>
    internal void ConfigureServices(ServiceConfigurationContext context)
    {
        foreach (var module in _modules)
        {
            module.Instance.ConfigureServices(context);
        }
    }

    /// <summary>
    /// Calls <see cref="FoundationModule.ConfigureServicesAsync"/> on each module
    /// in topological order (asynchronous version).
    /// </summary>
    internal async Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        foreach (var module in _modules)
        {
            await module.Instance.ConfigureServicesAsync(context);
        }
    }

    /// <summary>
    /// Calls <see cref="FoundationModule.OnApplicationInitialization"/> on each module
    /// in topological order (synchronous version).
    /// </summary>
    internal void InitializeApplication(ApplicationInitializationContext context)
    {
        foreach (var module in _modules)
        {
            module.Instance.OnApplicationInitialization(context);
        }
    }

    /// <summary>
    /// Calls <see cref="FoundationModule.OnApplicationInitializationAsync"/> on each module
    /// in topological order (asynchronous version).
    /// </summary>
    internal async Task InitializeApplicationAsync(ApplicationInitializationContext context)
    {
        foreach (var module in _modules)
        {
            await module.Instance.OnApplicationInitializationAsync(context);
        }
    }
}
