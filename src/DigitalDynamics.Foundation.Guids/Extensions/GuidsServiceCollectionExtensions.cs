using DigitalDynamics.Foundation.Guids;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DigitalDynamics.Foundation.Guids.Extensions;

/// <summary>
/// Extensions pour enregistrer les services du module Guids.
/// </summary>
public static class GuidsServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute les services du module Guids (IGuidGenerator avec GUID sequentiels).
    /// </summary>
    public static IServiceCollection AddFoundationGuids(
        this IServiceCollection services,
        Action<GuidGeneratorOptions>? configure = null)
    {
        // Singleton car SequentialGuidGenerator est thread-safe
        // (RandomNumberGenerator statique, IOptions injecte)
        services.TryAddSingleton<IGuidGenerator, SequentialGuidGenerator>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
