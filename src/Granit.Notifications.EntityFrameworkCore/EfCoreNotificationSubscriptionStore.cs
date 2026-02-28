using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Notifications.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of <see cref="INotificationSubscriptionStore"/> backed by PostgreSQL.
/// </summary>
internal sealed class EfCoreNotificationSubscriptionStore(IDbContextFactory<NotificationDbContext> dbContextFactory) : INotificationSubscriptionStore
{
    /// <inheritdoc/>
    public async Task SubscribeAsync(string userId, string notificationTypeName, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        bool exists = await db.Subscriptions.AnyAsync(s => s.UserId == userId && s.NotificationTypeName == notificationTypeName && s.TenantId == tenantId && s.EntityType == null, ct);
        if (!exists)
        {
            db.Subscriptions.Add(new NotificationSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NotificationTypeName = notificationTypeName,
                TenantId = tenantId,
            });
            await db.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task UnsubscribeAsync(string userId, string notificationTypeName, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        await db.Subscriptions
            .Where(s => s.UserId == userId && s.NotificationTypeName == notificationTypeName && s.TenantId == tenantId && s.EntityType == null)
            .ExecuteDeleteAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetSubscriberIdsAsync(string notificationTypeName, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.Subscriptions
            .Where(s => s.NotificationTypeName == notificationTypeName && s.TenantId == tenantId && s.EntityType == null)
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<NotificationSubscription>> GetUserSubscriptionsAsync(string userId, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.Subscriptions
            .Where(s => s.UserId == userId && s.TenantId == tenantId)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task FollowEntityAsync(string userId, string entityType, string entityId, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        bool exists = await db.Subscriptions.AnyAsync(s => s.UserId == userId && s.EntityType == entityType && s.EntityId == entityId && s.TenantId == tenantId, ct);
        if (!exists)
        {
            db.Subscriptions.Add(new NotificationSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NotificationTypeName = string.Empty,
                EntityType = entityType,
                EntityId = entityId,
                TenantId = tenantId,
            });
            await db.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task UnfollowEntityAsync(string userId, string entityType, string entityId, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        await db.Subscriptions
            .Where(s => s.UserId == userId && s.EntityType == entityType && s.EntityId == entityId && s.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetEntityFollowerIdsAsync(string entityType, string entityId, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.Subscriptions
            .Where(s => s.EntityType == entityType && s.EntityId == entityId && s.TenantId == tenantId)
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<NotificationSubscription>> GetEntityFollowersAsync(string entityType, string entityId, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.Subscriptions
            .Where(s => s.EntityType == entityType && s.EntityId == entityId && s.TenantId == tenantId)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<bool> IsFollowingEntityAsync(string userId, string entityType, string entityId, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.Subscriptions
            .AnyAsync(s => s.UserId == userId && s.EntityType == entityType && s.EntityId == entityId && s.TenantId == tenantId, ct);
    }
}
