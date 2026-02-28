using Granit.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Notifications.EntityFrameworkCore;

/// <summary>
/// EF Core DbContext for notification persistence.
/// </summary>
public sealed class NotificationDbContext : DbContext
{
    /// <summary>In-app user notifications (inbox).</summary>
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    /// <summary>Notification subscriptions (topic + entity followers).</summary>
    public DbSet<NotificationSubscription> Subscriptions => Set<NotificationSubscription>();

    /// <summary>User notification preferences (opt-in/opt-out per channel).</summary>
    public DbSet<NotificationPreference> Preferences => Set<NotificationPreference>();

    /// <summary>Immutable HDS audit trail of delivery attempts.</summary>
    public DbSet<NotificationDeliveryAttempt> DeliveryAttempts => Set<NotificationDeliveryAttempt>();

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDbContext"/> class.
    /// </summary>
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
    }
}
