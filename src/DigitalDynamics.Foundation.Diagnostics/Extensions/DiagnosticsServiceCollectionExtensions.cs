using Microsoft.Extensions.DependencyInjection;

namespace DigitalDynamics.Foundation.Diagnostics.Extensions;

/// <summary>
/// Extensions for registering Foundation diagnostics services.
/// </summary>
public static class DiagnosticsServiceCollectionExtensions
{
    /// <summary>
    /// Adds Foundation health check infrastructure (endpoint routing, response writer).
    /// Call <c>app.MapFoundationHealthChecks()</c> after <c>app.Build()</c> to expose the endpoints.
    /// </summary>
    public static IServiceCollection AddFoundationDiagnostics(
        this IServiceCollection services,
        Action<DiagnosticsOptions>? configure = null)
    {
        services.AddHealthChecks();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
