using Granit.Notifications.Abstractions;
using Granit.Notifications.Sse.Internal;
using Granit.Notifications.Sse.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Notifications.Sse.Extensions;

/// <summary>
/// Extension methods for registering the SSE notification channel.
/// </summary>
public static class SseNotificationsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the SSE real-time notification channel with optional configuration.
    /// </summary>
    public static IServiceCollection AddGranitNotificationsSse(
        this IServiceCollection services,
        Action<SseChannelOptions>? configure = null)
    {
        services.AddOptions<SseChannelOptions>()
            .BindConfiguration(SseChannelOptions.SectionName)
            .ValidateOnStart();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<ISseConnectionManager, SseConnectionManager>();
        services.AddSingleton<INotificationChannel, SseNotificationChannel>();

        return services;
    }
}
