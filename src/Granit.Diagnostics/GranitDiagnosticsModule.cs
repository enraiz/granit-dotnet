using Granit.Core.Modularity;
using Granit.Diagnostics.Extensions;

namespace Granit.Diagnostics;

/// <summary>
/// Granit module for Kubernetes health check infrastructure.
/// Registers <c>AddGranitDiagnostics()</c>; call <c>app.MapGranitHealthChecks()</c>
/// in <c>Program.cs</c> to expose the endpoints.
/// </summary>
public sealed class GranitDiagnosticsModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitDiagnostics();
}
