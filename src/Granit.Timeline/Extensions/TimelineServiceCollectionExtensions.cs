using Granit.Timeline.Abstractions;
using Granit.Timeline.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Timeline.Extensions;

/// <summary>
/// Extension methods for registering Granit.Timeline services.
/// </summary>
public static class TimelineServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Granit.Timeline activity stream engine with default in-memory stores.
    /// For production, call <c>AddGranitTimelineEntityFrameworkCore()</c>
    /// to enable durable PostgreSQL persistence.
    /// </summary>
    public static IServiceCollection AddGranitTimeline(this IServiceCollection services)
    {
        // Core stores (default: in-memory, replaced by EF Core package).
        // Scoped: depends on ICurrentUserService (scoped per-request).
        services.TryAddScoped<InMemoryTimelineStore>();
        services.TryAddScoped<ITimelineWriter>(sp => sp.GetRequiredService<InMemoryTimelineStore>());
        services.TryAddScoped<ITimelineReader, InMemoryTimelineQuery>();

        // Follower facade (standalone mode, replaced by Granit.Timeline.Notifications)
        services.TryAddScoped<ITimelineFollowerService, InMemoryTimelineFollowerService>();

        // Notifier facade (no-op, replaced by Granit.Timeline.Notifications)
        services.TryAddScoped<ITimelineNotifier, NullTimelineNotifier>();

        return services;
    }
}
