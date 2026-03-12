using System.Diagnostics;

namespace Granit.Notifications.Diagnostics;

/// <summary>
/// Central <see cref="ActivitySource"/> for Granit.Notifications distributed tracing.
/// </summary>
/// <remarks>
/// <c>Granit.Observability</c> adds this source automatically via
/// <c>GranitActivitySourceRegistry</c> when both packages are used.
/// </remarks>
internal static class NotificationsActivitySource
{
    /// <summary>The name of the Granit.Notifications <see cref="ActivitySource"/>.</summary>
    internal const string Name = "Granit.Notifications";

    /// <summary>The singleton <see cref="ActivitySource"/> instance.</summary>
    internal static readonly ActivitySource Source = new(Name);

    // ──── Operation names ────

    internal const string Deliver = "notifications.deliver";
    internal const string Fanout = "notifications.fanout";
}
