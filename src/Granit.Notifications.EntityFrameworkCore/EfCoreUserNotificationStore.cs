using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Granit.Querying;
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
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        db.UserNotifications.Add(notification);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<UserNotification?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await db.UserNotifications.FindAsync([id], ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<UserNotification>> GetListAsync(string recipientUserId, Guid? tenantId, int page = 1, int pageSize = QueryingDefaults.DefaultPageSize, CancellationToken ct = default)
    {
        int clampedPage = Math.Max(page, 1);
        int clampedPageSize = Math.Clamp(pageSize, 1, QueryingDefaults.MaxPageSize);

        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        IQueryable<UserNotification> query = db.UserNotifications
            .Where(n => n.RecipientUserId == recipientUserId && n.TenantId == tenantId);

        int totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        List<UserNotification> items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((clampedPage - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<UserNotification>(items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<int> GetUnreadCountAsync(string recipientUserId, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await db.UserNotifications
            .CountAsync(n => n.RecipientUserId == recipientUserId && n.TenantId == tenantId && n.State == UserNotificationState.Unread, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task MarkAsReadAsync(Guid id, DateTimeOffset readAt, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await db.UserNotifications
            .Where(n => n.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.State, UserNotificationState.Read)
                .SetProperty(n => n.ReadAt, readAt), ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task MarkAllAsReadAsync(string recipientUserId, Guid? tenantId, DateTimeOffset readAt, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await db.UserNotifications
            .Where(n => n.RecipientUserId == recipientUserId && n.TenantId == tenantId && n.State == UserNotificationState.Unread)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.State, UserNotificationState.Read)
                .SetProperty(n => n.ReadAt, readAt), ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<UserNotification>> GetByEntityAsync(string entityType, string entityId, Guid? tenantId, int page = 1, int pageSize = QueryingDefaults.DefaultPageSize, CancellationToken ct = default)
    {
        int clampedPage = Math.Max(page, 1);
        int clampedPageSize = Math.Clamp(pageSize, 1, QueryingDefaults.MaxPageSize);

        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        IQueryable<UserNotification> query = db.UserNotifications
            .Where(n => n.RelatedEntityType == entityType && n.RelatedEntityId == entityId && n.TenantId == tenantId);

        int totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        List<UserNotification> items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((clampedPage - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<UserNotification>(items, totalCount);
    }
}
