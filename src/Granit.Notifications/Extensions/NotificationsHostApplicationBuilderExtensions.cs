using Granit.Notifications.Abstractions;
using Granit.Notifications.Exceptions;
using Granit.Notifications.Internal;
using Granit.Notifications.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.ErrorHandling;

namespace Granit.Notifications.Extensions;

/// <summary>
/// Extension methods for registering Granit.Notifications services.
/// </summary>
public static class NotificationsHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Granit notification dispatch engine.
    /// </summary>
    public static IHostApplicationBuilder AddGranitNotifications(
        this IHostApplicationBuilder builder,
        Action<NotificationsOptions>? configure = null)
    {
        // Options
        builder.Services
            .AddOptions<NotificationsOptions>()
            .BindConfiguration(NotificationsOptions.SectionName)
            .ValidateOnStart();

        NotificationsOptions options = new();
        builder.Configuration.GetSection(NotificationsOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        // Default (replaceable) store registrations — CQRS forwarding pattern
        builder.Services.AddSingleton<InMemoryUserNotificationStore>();
        builder.Services.AddSingleton<IUserNotificationReader>(sp => sp.GetRequiredService<InMemoryUserNotificationStore>());
        builder.Services.AddSingleton<IUserNotificationWriter>(sp => sp.GetRequiredService<InMemoryUserNotificationStore>());

        builder.Services.AddSingleton<InMemoryNotificationPreferenceStore>();
        builder.Services.AddSingleton<INotificationPreferenceReader>(sp => sp.GetRequiredService<InMemoryNotificationPreferenceStore>());
        builder.Services.AddSingleton<INotificationPreferenceWriter>(sp => sp.GetRequiredService<InMemoryNotificationPreferenceStore>());

        builder.Services.AddSingleton<InMemoryNotificationSubscriptionStore>();
        builder.Services.AddSingleton<INotificationSubscriptionReader>(sp => sp.GetRequiredService<InMemoryNotificationSubscriptionStore>());
        builder.Services.AddSingleton<INotificationSubscriptionWriter>(sp => sp.GetRequiredService<InMemoryNotificationSubscriptionStore>());

        builder.Services.AddScoped<INotificationDeliveryWriter, NullNotificationDeliveryWriter>();

        // Definition store (singleton)
        builder.Services.AddSingleton<NotificationDefinitionStore>();
        builder.Services.AddSingleton<INotificationDefinitionStore>(sp => sp.GetRequiredService<NotificationDefinitionStore>());

        // Application facade
        builder.Services.AddScoped<INotificationPublisher, WolverineNotificationPublisher>();

        // InApp channel (built-in)
        builder.Services.AddSingleton<INotificationChannel, InAppNotificationChannel>();

        // Wolverine configuration
        builder.Services.ConfigureWolverine(opts =>
        {
            opts.LocalQueueFor<NotificationTrigger>()
                .Named("notification-fanout");

            opts.LocalQueueFor<DeliverNotificationCommand>()
                .Named("notification-delivery")
                .MaximumParallelMessages(options.MaxParallelDeliveries);

            opts.OnException<NotificationDeliveryException>()
                .RetryWithCooldown(
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(30),
                    TimeSpan.FromHours(2));
        });

        return builder;
    }

    /// <summary>
    /// Registers a notification definition provider.
    /// </summary>
    public static IServiceCollection AddNotificationDefinitions<TProvider>(this IServiceCollection services)
        where TProvider : class, INotificationDefinitionProvider
    {
        services.AddSingleton<INotificationDefinitionProvider, TProvider>();
        return services;
    }
}
