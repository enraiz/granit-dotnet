using Granit.Notifications.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Notifications.WhatsApp.Extensions;

/// <summary>Extension methods for the WhatsApp notification channel.</summary>
public static class WhatsAppNotificationsServiceCollectionExtensions
{
    /// <summary>Adds the WhatsApp notification channel with Keyed Services provider resolution.</summary>
    public static IServiceCollection AddGranitNotificationsWhatsApp(
        this IServiceCollection services,
        Action<WhatsAppChannelOptions>? configure = null)
    {
        services.AddOptions<WhatsAppChannelOptions>()
            .BindConfiguration(WhatsAppChannelOptions.SectionName)
            .ValidateOnStart();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddScoped<INotificationChannel, WhatsAppNotificationChannel>();
        return services;
    }
}
