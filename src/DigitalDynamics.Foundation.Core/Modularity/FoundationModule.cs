// =============================================================================
// FoundationModule - Classe de base pour les modules Foundation
// =============================================================================
// Chaque package Foundation declare un module qui s'enregistre dans le
// systeme de DI via ConfigureServices et s'initialise via
// OnApplicationInitialization.
//
// Lifecycle (sync + async) :
//   1. ConfigureServices / ConfigureServicesAsync
//   2. OnApplicationInitialization / OnApplicationInitializationAsync
//
// Les variantes async appellent par defaut la version sync.
// Un module peut surcharger l'une OU l'autre (pas les deux).
// AddFoundationAsync appelle les variantes async (donc passe aussi par sync).
//
// Extensibilite future (ajout sans breaking change) :
//   - PreConfigureServices / PostConfigureServices
//   - OnPreApplicationInitialization / OnPostApplicationInitialization
//   - OnApplicationShutdown / OnApplicationShutdownAsync
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Classe de base pour tous les modules Foundation.
/// Surcharger les methodes lifecycle (sync ou async) pour enregistrer
/// des services ou configurer le pipeline applicatif.
/// </summary>
public abstract class FoundationModule
{
    /// <summary>
    /// Enregistre les services du module dans le conteneur DI (version synchrone).
    /// Appele dans l'ordre topologique (dependances d'abord).
    /// </summary>
    public virtual void ConfigureServices(ServiceConfigurationContext context)
    {
    }

    /// <summary>
    /// Enregistre les services du module dans le conteneur DI (version asynchrone).
    /// Par defaut, appelle <see cref="ConfigureServices"/>.
    /// Surcharger cette methode pour les modules necessitant une configuration
    /// async (ex: lecture de secrets distants, verification de connectivite).
    /// </summary>
    public virtual Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        ConfigureServices(context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Initialise le module apres la construction de l'application (version synchrone).
    /// Appele apres <c>builder.Build()</c>, avant <c>app.Run()</c>.
    /// </summary>
    public virtual void OnApplicationInitialization(ApplicationInitializationContext context)
    {
    }

    /// <summary>
    /// Initialise le module apres la construction de l'application (version asynchrone).
    /// Par defaut, appelle <see cref="OnApplicationInitialization"/>.
    /// </summary>
    public virtual Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        OnApplicationInitialization(context);
        return Task.CompletedTask;
    }
}
