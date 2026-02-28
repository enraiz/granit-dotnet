using Granit.Notifications.Abstractions;
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
    /// backed by a PostgreSQL database hosted in Europe (OVHcloud FR).
    /// </summary>
    /// <remarks>
    /// Must be called after <c>AddGranitNotifications()</c>.
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="EfCoreUserNotificationStore"/> — replaces <c>InMemoryUserNotificationStore</c>.</item>
    ///   <item><see cref="EfCoreNotificationPreferenceStore"/> — replaces <c>InMemoryNotificationPreferenceStore</c>.</item>
    ///   <item><see cref="EfCoreNotificationSubscriptionStore"/> — replaces <c>InMemoryNotificationSubscriptionStore</c>.</item>
    ///   <item><see cref="EfCoreNotificationDeliveryStore"/> — replaces <c>NullNotificationDeliveryStore</c> (enables HDS audit trail).</item>
    ///   <item><see cref="NotificationDbContext"/> — registered via <c>IDbContextFactory</c> for thread-safe usage in Wolverine handlers.</item>
    /// </list>
    /// <para>
    /// SOVEREIGNTY: The connection string must point to a database hosted in Europe (OVHcloud FR).
    /// Never use AWS RDS, Azure SQL, or Google Cloud SQL for health data.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">EF Core <see cref="DbContextOptionsBuilder"/> configuration (provider + connection string).</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitNotificationsEntityFrameworkCore(
        this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder> configure)
    {
        builder.Services.AddDbContextFactory<NotificationDbContext>(configure);

        builder.Services.Replace(
            ServiceDescriptor.Singleton<IUserNotificationStore, EfCoreUserNotificationStore>());
        builder.Services.Replace(
            ServiceDescriptor.Singleton<INotificationPreferenceStore, EfCoreNotificationPreferenceStore>());
        builder.Services.Replace(
            ServiceDescriptor.Singleton<INotificationSubscriptionStore, EfCoreNotificationSubscriptionStore>());
        builder.Services.Replace(
            ServiceDescriptor.Scoped<INotificationDeliveryStore, EfCoreNotificationDeliveryStore>());

        return builder;
    }
}
