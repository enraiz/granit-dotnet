// =============================================================================
// FoundationModule - Base class for Foundation modules
// =============================================================================
// Each Foundation package declares a module that registers itself in the
// DI system via ConfigureServices and initializes via
// OnApplicationInitialization.
//
// Lifecycle (sync + async):
//   1. ConfigureServices / ConfigureServicesAsync
//   2. OnApplicationInitialization / OnApplicationInitializationAsync
//
// Async variants call the sync version by default.
// A module may override one OR the other (not both).
// AddFoundationAsync calls the async variants (which also go through sync).
//
// Future extensibility (additions without breaking changes):
//   - PreConfigureServices / PostConfigureServices
//   - OnPreApplicationInitialization / OnPostApplicationInitialization
//   - OnApplicationShutdown / OnApplicationShutdownAsync
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Base class for all Foundation modules.
/// Override lifecycle methods (sync or async) to register
/// services or configure the application pipeline.
/// </summary>
public abstract class FoundationModule
{
    /// <summary>
    /// Registers the module's services in the DI container (synchronous version).
    /// Called in topological order (dependencies first).
    /// </summary>
    public virtual void ConfigureServices(ServiceConfigurationContext context)
    {
    }

    /// <summary>
    /// Registers the module's services in the DI container (asynchronous version).
    /// By default, calls <see cref="ConfigureServices"/>.
    /// Override this method for modules requiring async configuration
    /// (e.g. reading remote secrets, verifying connectivity).
    /// </summary>
    public virtual Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        ConfigureServices(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes the module after the application is built (synchronous version).
    /// Called after <c>builder.Build()</c>, before <c>app.Run()</c>.
    /// </summary>
    public virtual void OnApplicationInitialization(ApplicationInitializationContext context)
    {
    }

    /// <summary>
    /// Initializes the module after the application is built (asynchronous version).
    /// By default, calls <see cref="OnApplicationInitialization"/>.
    /// </summary>
    public virtual Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        OnApplicationInitialization(context);
        return Task.CompletedTask;
    }
}
