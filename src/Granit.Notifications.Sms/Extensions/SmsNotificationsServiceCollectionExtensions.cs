using Granit.Notifications.Abstractions;
using Granit.Notifications.Sms.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Notifications.Sms.Extensions;

/// <summary>Extension methods for the SMS notification channel.</summary>
public static class SmsNotificationsServiceCollectionExtensions
{
    /// <summary>Adds the SMS notification channel with Keyed Services provider resolution.</summary>
    public static IServiceCollection AddGranitNotificationsSms(
        this IServiceCollection services,
        Action<SmsChannelOptions>? configure = null)
    {
        services.AddOptions<SmsChannelOptions>()
            .BindConfiguration(SmsChannelOptions.SectionName)
            .ValidateOnStart();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddScoped<INotificationChannel, SmsNotificationChannel>();
        return services;
    }
}
