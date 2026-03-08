using Granit.Notifications.MobilePush;
using Microsoft.EntityFrameworkCore;

namespace Granit.Notifications.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of <see cref="IMobilePushTokenReader"/> and <see cref="IMobilePushTokenWriter"/>.
/// </summary>
internal sealed class EfCoreMobilePushTokenStore(
    IDbContextFactory<NotificationDbContext> dbContextFactory) : IMobilePushTokenReader, IMobilePushTokenWriter
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MobilePushTokenInfo>> GetTokensAsync(
        string userId, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        return await db.MobilePushTokens
            .Where(t => t.UserId == userId && t.TenantId == tenantId)
            .Select(t => t.ToTokenInfo())
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RegisterAsync(MobilePushTokenInfo tokenInfo, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        MobilePushTokenEntity? existing = await db.MobilePushTokens
            .FirstOrDefaultAsync(t => t.DeviceToken == tokenInfo.DeviceToken && t.TenantId == tokenInfo.TenantId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.UserId = tokenInfo.UserId;
            existing.Platform = tokenInfo.Platform;
        }
        else
        {
            db.MobilePushTokens.Add(new MobilePushTokenEntity
            {
                UserId = tokenInfo.UserId,
                DeviceToken = tokenInfo.DeviceToken,
                Platform = tokenInfo.Platform,
                TenantId = tokenInfo.TenantId,
            });
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string deviceToken, Guid? tenantId, CancellationToken ct = default)
    {
        await using NotificationDbContext db = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        await db.MobilePushTokens
            .Where(t => t.DeviceToken == deviceToken && t.TenantId == tenantId)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);
    }
}
