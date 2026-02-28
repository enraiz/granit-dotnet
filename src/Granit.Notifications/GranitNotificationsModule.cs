using Granit.Core.Modularity;
using Granit.Notifications.Extensions;
using Granit.Timing;
using Granit.Wolverine;

namespace Granit.Notifications;

/// <summary>
/// Granit module for the multi-channel notification engine.
/// </summary>
/// <remarks>
/// Default registrations use in-memory stores suitable for development and tests.
/// For production, call <c>AddGranitNotificationsEntityFrameworkCore()</c>
/// to enable durable persistence and the HDS-compliant audit trail.
/// </remarks>
[DependsOn(typeof(GranitTimingModule))]
[DependsOn(typeof(GranitWolverineModule))]
public sealed class GranitNotificationsModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Builder.AddGranitNotifications();
}
