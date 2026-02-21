// =============================================================================
// ServiceConfigurationContext - Contexte pour ConfigureServices
// =============================================================================
// Passe a chaque module lors de la phase d'enregistrement DI.
// Fournit acces au conteneur de services, a la configuration, et au builder.
// =============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Contexte fourni a <see cref="FoundationModule.ConfigureServices"/>.
/// </summary>
public sealed class ServiceConfigurationContext
{
    /// <summary>Collection de services pour l'enregistrement DI.</summary>
    public IServiceCollection Services { get; }

    /// <summary>Configuration de l'application (appsettings, variables d'environnement).</summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// Builder complet de l'application. Necessaire pour certains modules
    /// (ex: Observability utilise <c>builder.Host.UseSerilog()</c>).
    /// </summary>
    public IHostApplicationBuilder Builder { get; }

    /// <summary>
    /// Dictionnaire d'etat partage pour la communication inter-modules
    /// pendant la phase ConfigureServices.
    /// </summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    public ServiceConfigurationContext(
        IServiceCollection services,
        IConfiguration configuration,
        IHostApplicationBuilder builder)
    {
        Services = services;
        Configuration = configuration;
        Builder = builder;
    }
}
