using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Notifications.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of <see cref="INotificationPreferenceStore"/> backed by PostgreSQL.
/// </summary>
internal sealed class EfCoreNotificationPreferenceStore(IDbContextFactory<NotificationDbContext> dbContextFactory) : INotificationPreferenceStore
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<NotificationPreference>> GetListAsync(string userId, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.Preferences
            .Where(p => p.UserId == userId && p.TenantId == tenantId)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<NotificationPreference?> GetAsync(string userId, string notificationTypeName, string channelName, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.Preferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationTypeName == notificationTypeName && p.ChannelName == channelName && p.TenantId == tenantId, ct);
    }

    /// <inheritdoc/>
    public async Task SetAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        NotificationPreference? existing = await db.Preferences
            .FirstOrDefaultAsync(p => p.UserId == preference.UserId && p.NotificationTypeName == preference.NotificationTypeName && p.ChannelName == preference.ChannelName && p.TenantId == preference.TenantId, ct);

        if (existing is not null)
        {
            existing.IsEnabled = preference.IsEnabled;
            existing.ModifiedAt = preference.ModifiedAt;
            existing.ModifiedBy = preference.ModifiedBy;
        }
        else
        {
            db.Preferences.Add(preference);
        }

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<bool> IsChannelEnabledAsync(string userId, string notificationTypeName, string channelName, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct);
        NotificationPreference? preference = await db.Preferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationTypeName == notificationTypeName && p.ChannelName == channelName && p.TenantId == tenantId, ct);
        return preference?.IsEnabled ?? true; // Default: enabled
    }
}
