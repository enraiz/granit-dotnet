using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Observability.Extensions;

namespace DigitalDynamics.Foundation.Observability;

/// <summary>
/// Module Foundation pour Serilog + OpenTelemetry.
/// Utilise context.Builder car Serilog necessite IHostApplicationBuilder.
/// </summary>
public sealed class FoundationObservabilityModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Builder.AddFoundationObservability();
}
