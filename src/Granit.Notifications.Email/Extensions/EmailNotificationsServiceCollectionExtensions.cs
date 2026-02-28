using Granit.Notifications.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.Notifications.Email.Extensions;

/// <summary>Extension methods for the email notification channel.</summary>
public static class EmailNotificationsServiceCollectionExtensions
{
    /// <summary>Adds the email notification channel with Keyed Services provider resolution.</summary>
    public static IServiceCollection AddGranitNotificationsEmail(
        this IServiceCollection services,
        Action<EmailChannelOptions>? configure = null)
    {
        services.AddOptions<EmailChannelOptions>()
            .BindConfiguration(EmailChannelOptions.SectionName)
            .ValidateOnStart();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<INotificationChannel, EmailNotificationChannel>();
        return services;
    }
}
