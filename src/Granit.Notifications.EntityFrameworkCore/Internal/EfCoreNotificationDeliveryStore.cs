using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Notifications.EntityFrameworkCore.Internal;

/// <summary>
/// INSERT-only HDS-compliant audit store for delivery attempts.
/// </summary>
/// <remarks>
/// HDS compliance: <see cref="NotificationDeliveryAttempt"/> records are INSERT-only.
/// This store never updates or deletes them.
/// </remarks>
internal sealed class EfCoreNotificationDeliveryStore(IDbContextFactory<NotificationDbContext> dbContextFactory) : INotificationDeliveryWriter
{
    /// <inheritdoc/>
    public async Task RecordAsync(NotificationDeliveryAttempt attempt, CancellationToken cancellationToken = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        db.DeliveryAttempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
