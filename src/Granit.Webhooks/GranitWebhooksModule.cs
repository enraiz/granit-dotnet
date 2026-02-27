using Granit.Core.Modularity;
using Granit.Timing;
using Granit.Webhooks.Extensions;
using Granit.Wolverine;

namespace Granit.Webhooks;

/// <summary>
/// Granit module for outbound webhook dispatch.
/// </summary>
/// <remarks>
/// Registers the webhook engine on top of <see cref="GranitWolverineModule"/>.
/// <para>
/// Default registrations use in-memory and no-op stores suitable for development and tests.
/// For production, call <c>AddGranitWebhooksEntityFrameworkCore()</c>
/// from <c>Granit.Webhooks.EntityFrameworkCore</c> to enable durable persistence and
/// the HDS-compliant audit trail.
/// </para>
/// </remarks>
[DependsOn(typeof(GranitTimingModule))]
[DependsOn(typeof(GranitWolverineModule))]
public sealed class GranitWebhooksModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Builder.AddGranitWebhooks();
}
