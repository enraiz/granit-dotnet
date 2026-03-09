using Granit.Core.DataFiltering;
using Granit.Core.MultiTenancy;
using Granit.Notifications.Domain;
using Granit.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Granit.Notifications.EntityFrameworkCore;

/// <summary>
/// EF Core DbContext for notification persistence.
/// </summary>
public sealed class NotificationDbContext : DbContext
{
    private readonly ICurrentTenant? _currentTenant;
    private readonly IDataFilter? _dataFilter;

    /// <summary>In-app user notifications (inbox).</summary>
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    /// <summary>Notification subscriptions (topic + entity followers).</summary>
    public DbSet<NotificationSubscription> Subscriptions => Set<NotificationSubscription>();

    /// <summary>User notification preferences (opt-in/opt-out per channel).</summary>
    public DbSet<NotificationPreference> Preferences => Set<NotificationPreference>();

    /// <summary>Immutable HDS audit trail of delivery attempts.</summary>
    public DbSet<NotificationDeliveryAttempt> DeliveryAttempts => Set<NotificationDeliveryAttempt>();

    /// <summary>Mobile push device tokens.</summary>
    public DbSet<MobilePushTokenEntity> MobilePushTokens => Set<MobilePushTokenEntity>();

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationDbContext"/> class.
    /// </summary>
    public NotificationDbContext(
        DbContextOptions<NotificationDbContext> options,
        ICurrentTenant? currentTenant = null,
        IDataFilter? dataFilter = null) : base(options)
    {
        _currentTenant = currentTenant;
        _dataFilter = dataFilter;
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
        modelBuilder.ApplyGranitConventions(_currentTenant, _dataFilter);
    }
}
