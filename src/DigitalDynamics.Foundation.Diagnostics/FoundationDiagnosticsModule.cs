using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Diagnostics.Extensions;

namespace DigitalDynamics.Foundation.Diagnostics;

/// <summary>
/// Foundation module for Kubernetes health check infrastructure.
/// Registers <c>AddFoundationDiagnostics()</c>; call <c>app.MapFoundationHealthChecks()</c>
/// in <c>Program.cs</c> to expose the endpoints.
/// </summary>
public sealed class FoundationDiagnosticsModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationDiagnostics();
}
