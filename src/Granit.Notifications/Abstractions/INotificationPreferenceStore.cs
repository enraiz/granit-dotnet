using Granit.Notifications.Domain;

namespace Granit.Notifications.Abstractions;

/// <summary>
/// Store for user notification preferences (opt-in/opt-out per channel/type).
/// </summary>
public interface INotificationPreferenceStore
{
    Task<IReadOnlyList<NotificationPreference>> GetListAsync(string userId, Guid? tenantId, CancellationToken ct = default);
    Task<NotificationPreference?> GetAsync(string userId, string notificationTypeName, string channelName, Guid? tenantId, CancellationToken ct = default);
    Task SetAsync(NotificationPreference preference, CancellationToken ct = default);
    Task<bool> IsChannelEnabledAsync(string userId, string notificationTypeName, string channelName, Guid? tenantId, CancellationToken ct = default);
}
