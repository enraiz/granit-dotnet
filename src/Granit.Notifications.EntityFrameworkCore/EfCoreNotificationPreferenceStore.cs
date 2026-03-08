using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Notifications.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of <see cref="INotificationPreferenceReader"/> and
/// <see cref="INotificationPreferenceWriter"/> backed by PostgreSQL.
/// </summary>
internal sealed class EfCoreNotificationPreferenceStore(IDbContextFactory<NotificationDbContext> dbContextFactory) : INotificationPreferenceReader, INotificationPreferenceWriter
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<NotificationPreference>> GetListAsync(string userId, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await db.Preferences
            .Where(p => p.UserId == userId && p.TenantId == tenantId)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<NotificationPreference?> GetAsync(string userId, string notificationTypeName, string channelName, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await db.Preferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationTypeName == notificationTypeName && p.ChannelName == channelName && p.TenantId == tenantId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        NotificationPreference? existing = await db.Preferences
            .FirstOrDefaultAsync(p => p.UserId == preference.UserId && p.NotificationTypeName == preference.NotificationTypeName && p.ChannelName == preference.ChannelName && p.TenantId == preference.TenantId, ct).ConfigureAwait(false);

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

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> IsChannelEnabledAsync(string userId, string notificationTypeName, string channelName, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        NotificationPreference? preference = await db.Preferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationTypeName == notificationTypeName && p.ChannelName == channelName && p.TenantId == tenantId, ct).ConfigureAwait(false);
        return preference?.IsEnabled ?? true; // Default: enabled
    }
}
