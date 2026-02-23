using Granit.Guids;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Guids.Extensions;

/// <summary>
/// Extensions for registering Guids module services.
/// </summary>
public static class GuidsServiceCollectionExtensions
{
    /// <summary>
    /// Adds Guids module services (IGuidGenerator with sequential GUIDs).
    /// </summary>
    public static IServiceCollection AddGranitGuids(
        this IServiceCollection services,
        Action<GuidGeneratorOptions>? configure = null)
    {
        // Singleton because SequentialGuidGenerator is thread-safe
        // (static RandomNumberGenerator, injected IOptions)
        services.TryAddSingleton<IGuidGenerator, SequentialGuidGenerator>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
