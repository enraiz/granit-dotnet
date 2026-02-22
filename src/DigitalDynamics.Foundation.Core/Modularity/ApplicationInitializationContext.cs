namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Contexte fourni a <see cref="FoundationModule.OnApplicationInitialization"/>.
/// </summary>
public sealed class ApplicationInitializationContext(IServiceProvider serviceProvider)
{
    /// <summary>Fournisseur de services resolus (apres Build()).</summary>
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
}
