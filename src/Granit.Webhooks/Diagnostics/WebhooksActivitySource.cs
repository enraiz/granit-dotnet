using System.Diagnostics;

namespace Granit.Webhooks.Diagnostics;

/// <summary>
/// Central <see cref="ActivitySource"/> for Granit.Webhooks distributed tracing.
/// </summary>
/// <remarks>
/// <c>Granit.Observability</c> adds this source automatically via
/// <c>GranitActivitySourceRegistry</c> when both packages are used.
/// </remarks>
internal static class WebhooksActivitySource
{
    /// <summary>The name of the Granit.Webhooks <see cref="ActivitySource"/>.</summary>
    internal const string Name = "Granit.Webhooks";

    /// <summary>The singleton <see cref="ActivitySource"/> instance.</summary>
    internal static readonly ActivitySource Source = new(Name);

    // ──── Operation names ────

    internal const string Deliver = "webhooks.deliver";
    internal const string Fanout = "webhooks.fanout";
}
