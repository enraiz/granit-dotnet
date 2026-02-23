using Granit.Core.Modularity;
using Granit.Observability.Extensions;

namespace Granit.Observability;

/// <summary>
/// Module Granit pour Serilog + OpenTelemetry.
/// Utilise context.Builder car Serilog necessite IHostApplicationBuilder.
/// </summary>
public sealed class GranitObservabilityModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Builder.AddGranitObservability();
}
