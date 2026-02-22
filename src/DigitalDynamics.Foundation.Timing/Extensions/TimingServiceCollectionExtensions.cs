using DigitalDynamics.Foundation.Timing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DigitalDynamics.Foundation.Timing.Extensions;

/// <summary>
/// Extensions pour enregistrer les services du module Timing.
/// </summary>
public static class TimingServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute les services du module Timing (IClock, ICurrentTimezoneProvider, TimeProvider).
    /// </summary>
    public static IServiceCollection AddFoundationTiming(
        this IServiceCollection services,
        Action<ClockOptions>? configure = null)
    {
        // TimeProvider.System est le provider standard .NET (thread-safe, stateless)
        services.TryAddSingleton(TimeProvider.System);

        // Singleton + AsyncLocal : le runtime isole la valeur par contexte async
        services.TryAddSingleton<ICurrentTimezoneProvider, CurrentTimezoneProvider>();

        // Singleton car Clock est stateless (TimeProvider.System est thread-safe)
        services.TryAddSingleton<IClock, Clock>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
