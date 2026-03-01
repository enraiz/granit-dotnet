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
        // Core stores (default: in-memory, replaced by EF Core package)
        services.TryAddSingleton<InMemoryTimelineStore>();
        services.TryAddSingleton<ITimelineStore>(sp => sp.GetRequiredService<InMemoryTimelineStore>());
        services.TryAddSingleton<ITimelineQuery, InMemoryTimelineQuery>();

        // Follower facade (standalone mode, replaced by Granit.Timeline.Notifications)
        services.TryAddSingleton<ITimelineFollowerService, InMemoryTimelineFollowerService>();

        // Notifier facade (no-op, replaced by Granit.Timeline.Notifications)
        services.TryAddSingleton<ITimelineNotifier, NullTimelineNotifier>();

        return services;
    }
}
