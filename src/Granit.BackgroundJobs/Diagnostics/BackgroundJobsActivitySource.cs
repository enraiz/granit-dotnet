using System.Diagnostics;

namespace Granit.BackgroundJobs.Diagnostics;

/// <summary>
/// Central <see cref="ActivitySource"/> for Granit.BackgroundJobs distributed tracing.
/// </summary>
/// <remarks>
/// <c>Granit.Observability</c> adds this source automatically via
/// <c>GranitActivitySourceRegistry</c> when both packages are used.
/// </remarks>
internal static class BackgroundJobsActivitySource
{
    /// <summary>The name of the Granit.BackgroundJobs <see cref="ActivitySource"/>.</summary>
    internal const string Name = "Granit.BackgroundJobs";

    /// <summary>The singleton <see cref="ActivitySource"/> instance.</summary>
    internal static readonly ActivitySource Source = new(Name);

    // ──── Operation names ────

    internal const string Trigger = "backgroundjobs.trigger";
}
