namespace Granit.Notifications.MobilePush;

/// <summary>Reads mobile push device tokens.</summary>
public interface IMobilePushTokenReader
{
    /// <summary>Gets all device tokens for a user.</summary>
    Task<IReadOnlyList<MobilePushTokenInfo>> GetTokensAsync(string userId, Guid? tenantId, CancellationToken ct = default);
}
