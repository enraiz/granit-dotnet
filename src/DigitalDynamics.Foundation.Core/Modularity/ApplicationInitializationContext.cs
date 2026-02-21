// =============================================================================
// ApplicationInitializationContext - Contexte pour OnApplicationInitialization
// =============================================================================
// Passe a chaque module apres builder.Build(), avant app.Run().
// Permet d'acceder aux services resolus.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Contexte fourni a <see cref="FoundationModule.OnApplicationInitialization"/>.
/// </summary>
public sealed class ApplicationInitializationContext
{
    /// <summary>Fournisseur de services resolus (apres Build()).</summary>
    public IServiceProvider ServiceProvider { get; }

    public ApplicationInitializationContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}
