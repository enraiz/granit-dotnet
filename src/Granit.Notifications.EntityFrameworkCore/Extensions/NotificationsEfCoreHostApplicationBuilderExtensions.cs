using Granit.Notifications.Abstractions;
using Granit.Notifications.EntityFrameworkCore.Internal;
using Granit.Notifications.Internal;
using Granit.Notifications.MobilePush;
using Granit.Notifications.MobilePush.Internal;
using Granit.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Granit.Notifications.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for enabling EF Core persistence in Granit.Notifications.
/// </summary>
public static class NotificationsEfCoreHostApplicationBuilderExtensions
{
    /// <summary>
    /// Replaces the default InMemory/no-op stores with durable EF Core implementations
    /// backed by a PostgreSQL database.
    /// </summary>
    /// <remarks>
    /// Must be called after <c>AddGranitNotifications()</c>.
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="EfCoreUserNotificationStore"/> — replaces <c>InMemoryUserNotificationStore</c>.</item>
    ///   <item><see cref="EfCoreNotificationPreferenceStore"/> — replaces <c>InMemoryNotificationPreferenceStore</c>.</item>
    ///   <item><see cref="EfCoreNotificationSubscriptionStore"/> — replaces <c>InMemoryNotificationSubscriptionStore</c>.</item>
    ///   <item><see cref="EfCoreNotificationDeliveryStore"/> — replaces <c>NullNotificationDeliveryStore</c> (enables ISO 27001 audit trail).</item>
    ///   <item><see cref="EfCoreMobilePushTokenStore"/> — replaces <c>InMemoryMobilePushTokenStore</c>.</item>
    ///   <item><see cref="Internal.NotificationDbContext"/> — registered via <c>IDbContextFactory</c> for thread-safe usage in Wolverine handlers.</item>
    /// </list>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">EF Core <see cref="DbContextOptionsBuilder"/> configuration (provider + connection string).</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitNotificationsEntityFrameworkCore(
        this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder> configure)
    {
        builder.Services.AddDbContextFactory<NotificationDbContext>((sp, options) =>
        {
            configure(options);
            options.UseGranitInterceptors(sp);
        }, ServiceLifetime.Scoped);

        // UserNotification store — CQRS forwarding pattern
        builder.Services.RemoveAll<InMemoryUserNotificationStore>();
        builder.Services.AddSingleton<EfCoreUserNotificationStore>();
        builder.Services.Replace(
            ServiceDescriptor.Singleton<IUserNotificationReader>(sp => sp.GetRequiredService<EfCoreUserNotificationStore>()));
        builder.Services.Replace(
            ServiceDescriptor.Singleton<IUserNotificationWriter>(sp => sp.GetRequiredService<EfCoreUserNotificationStore>()));

        // Preference store — CQRS forwarding pattern
        builder.Services.RemoveAll<InMemoryNotificationPreferenceStore>();
        builder.Services.AddSingleton<EfCoreNotificationPreferenceStore>();
        builder.Services.Replace(
            ServiceDescriptor.Singleton<INotificationPreferenceReader>(sp => sp.GetRequiredService<EfCoreNotificationPreferenceStore>()));
        builder.Services.Replace(
            ServiceDescriptor.Singleton<INotificationPreferenceWriter>(sp => sp.GetRequiredService<EfCoreNotificationPreferenceStore>()));

        // Subscription store — CQRS forwarding pattern
        builder.Services.RemoveAll<InMemoryNotificationSubscriptionStore>();
        builder.Services.AddSingleton<EfCoreNotificationSubscriptionStore>();
        builder.Services.Replace(
            ServiceDescriptor.Singleton<INotificationSubscriptionReader>(sp => sp.GetRequiredService<EfCoreNotificationSubscriptionStore>()));
        builder.Services.Replace(
            ServiceDescriptor.Singleton<INotificationSubscriptionWriter>(sp => sp.GetRequiredService<EfCoreNotificationSubscriptionStore>()));

        // Delivery store — write-only (ISO 27001 audit)
        builder.Services.Replace(
            ServiceDescriptor.Scoped<INotificationDeliveryWriter, EfCoreNotificationDeliveryStore>());

        // MobilePush token store — CQRS forwarding pattern
        builder.Services.RemoveAll<InMemoryMobilePushTokenStore>();
        builder.Services.AddSingleton<EfCoreMobilePushTokenStore>();
        builder.Services.Replace(
            ServiceDescriptor.Singleton<IMobilePushTokenReader>(sp => sp.GetRequiredService<EfCoreMobilePushTokenStore>()));
        builder.Services.Replace(
            ServiceDescriptor.Singleton<IMobilePushTokenWriter>(sp => sp.GetRequiredService<EfCoreMobilePushTokenStore>()));

        return builder;
    }
}
