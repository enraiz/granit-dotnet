using Granit.Timeline.Abstractions;
using Granit.Timeline.Notifications.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Timeline.Notifications.Extensions;

/// <summary>
/// Extension methods for registering Granit.Timeline.Notifications services.
/// </summary>
public static class TimelineNotificationsServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default in-memory follower service and null notifier with
    /// notification-backed implementations that delegate to <c>Granit.Notifications</c>.
    /// </summary>
    /// <remarks>
    /// Must be called after both <c>AddGranitTimeline()</c> and <c>AddGranitNotifications()</c>.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitTimelineNotifications(
        this IServiceCollection services)
    {
        services.Replace(
            ServiceDescriptor.Singleton<ITimelineFollowerService, NotificationBackedFollowerService>());
        services.Replace(
            ServiceDescriptor.Singleton<ITimelineNotifier, NotificationBackedNotifier>());

        return services;
    }
}
