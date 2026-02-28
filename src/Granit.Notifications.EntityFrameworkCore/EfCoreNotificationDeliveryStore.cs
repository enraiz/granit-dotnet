using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Notifications.EntityFrameworkCore;

/// <summary>
/// INSERT-only HDS-compliant audit store for delivery attempts.
/// </summary>
/// <remarks>
/// HDS compliance: <see cref="NotificationDeliveryAttempt"/> records are INSERT-only.
/// This store never updates or deletes them.
/// </remarks>
internal sealed class EfCoreNotificationDeliveryStore(IDbContextFactory<NotificationDbContext> dbContextFactory) : INotificationDeliveryStore
{
    /// <inheritdoc/>
    public async Task RecordAsync(NotificationDeliveryAttempt attempt, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        db.DeliveryAttempts.Add(attempt);
        await db.SaveChangesAsync(ct);
    }
}
