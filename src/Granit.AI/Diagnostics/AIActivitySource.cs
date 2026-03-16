using System.Diagnostics;

namespace Granit.AI.Diagnostics;

/// <summary>
/// OpenTelemetry activity source for the Granit AI module.
/// </summary>
internal static class AIActivitySource
{
    public const string Name = "Granit.AI";

    internal static readonly ActivitySource Instance = new(Name);
}
