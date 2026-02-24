using System.Diagnostics;

namespace Granit.Wolverine.Diagnostics;

/// <summary>
/// Central <see cref="ActivitySource"/> for Granit.Wolverine distributed tracing.
/// </summary>
/// <remarks>
/// Register this source in the OpenTelemetry tracer provider (via
/// <c>AddSource(WolverineActivitySource.Name)</c>) to capture the Wolverine
/// message-handling bridge spans in Tempo/Grafana.
/// <para>
/// <c>Granit.Observability</c> adds this source automatically when both packages are used.
/// </para>
/// </remarks>
internal static class WolverineActivitySource
{
    /// <summary>The name of the Granit.Wolverine <see cref="ActivitySource"/>.</summary>
    internal const string Name = "Granit.Wolverine";

    /// <summary>The singleton <see cref="ActivitySource"/> instance.</summary>
    internal static readonly ActivitySource Source = new(Name);
}
