namespace Granit.Notifications.MobilePush;

/// <summary>Writes (registers/removes) mobile push device tokens.</summary>
public interface IMobilePushTokenWriter
{
    /// <summary>Registers or updates a device token for a user.</summary>
    Task RegisterAsync(MobilePushTokenInfo tokenInfo, CancellationToken ct = default);

    /// <summary>Removes a device token.</summary>
    Task RemoveAsync(string deviceToken, Guid? tenantId, CancellationToken ct = default);
}
