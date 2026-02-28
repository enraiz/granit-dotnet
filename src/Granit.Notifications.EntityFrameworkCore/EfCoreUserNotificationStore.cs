using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Notifications.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of <see cref="IUserNotificationStore"/> backed by PostgreSQL.
/// </summary>
internal sealed class EfCoreUserNotificationStore(IDbContextFactory<NotificationDbContext> dbContextFactory) : IUserNotificationStore
{
    /// <inheritdoc/>
    public async Task InsertAsync(UserNotification notification, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        db.UserNotifications.Add(notification);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<UserNotification?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.UserNotifications.FindAsync([id], ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserNotification>> GetListAsync(string recipientUserId, Guid? tenantId, int skipCount, int maxResultCount, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.UserNotifications
            .Where(n => n.RecipientUserId == recipientUserId && n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<int> GetUnreadCountAsync(string recipientUserId, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.UserNotifications
            .CountAsync(n => n.RecipientUserId == recipientUserId && n.TenantId == tenantId && n.State == UserNotificationState.Unread, ct);
    }

    /// <inheritdoc/>
    public async Task MarkAsReadAsync(Guid id, DateTimeOffset readAt, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        await db.UserNotifications
            .Where(n => n.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.State, UserNotificationState.Read)
                .SetProperty(n => n.ReadAt, readAt), ct);
    }

    /// <inheritdoc/>
    public async Task MarkAllAsReadAsync(string recipientUserId, Guid? tenantId, DateTimeOffset readAt, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        await db.UserNotifications
            .Where(n => n.RecipientUserId == recipientUserId && n.TenantId == tenantId && n.State == UserNotificationState.Unread)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.State, UserNotificationState.Read)
                .SetProperty(n => n.ReadAt, readAt), ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserNotification>> GetByEntityAsync(string entityType, string entityId, Guid? tenantId, int skipCount, int maxResultCount, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.UserNotifications
            .Where(n => n.RelatedEntityType == entityType && n.RelatedEntityId == entityId && n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync(ct);
    }
}
