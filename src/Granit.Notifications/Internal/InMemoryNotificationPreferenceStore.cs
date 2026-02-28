using System.Collections.Concurrent;
using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;

namespace Granit.Notifications.Internal;

internal sealed class InMemoryNotificationPreferenceStore : INotificationPreferenceStore
{
    private readonly ConcurrentDictionary<string, NotificationPreference> _preferences = new();

    private static string BuildKey(string userId, string typeName, string channelName, Guid? tenantId) =>
        $"{tenantId}:{userId}:{typeName}:{channelName}";

    public Task<IReadOnlyList<NotificationPreference>> GetListAsync(string userId, Guid? tenantId, CancellationToken ct = default)
    {
        IReadOnlyList<NotificationPreference> result = _preferences.Values
            .Where(p => p.UserId == userId && p.TenantId == tenantId)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<NotificationPreference?> GetAsync(string userId, string notificationTypeName, string channelName, Guid? tenantId, CancellationToken ct = default)
    {
        string key = BuildKey(userId, notificationTypeName, channelName, tenantId);
        _preferences.TryGetValue(key, out NotificationPreference? preference);
        return Task.FromResult(preference);
    }

    public Task SetAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        string key = BuildKey(preference.UserId, preference.NotificationTypeName, preference.ChannelName, preference.TenantId);
        _preferences[key] = preference;
        return Task.CompletedTask;
    }

    public Task<bool> IsChannelEnabledAsync(string userId, string notificationTypeName, string channelName, Guid? tenantId, CancellationToken ct = default)
    {
        string key = BuildKey(userId, notificationTypeName, channelName, tenantId);
        if (_preferences.TryGetValue(key, out NotificationPreference? preference))
        {
            return Task.FromResult(preference.IsEnabled);
        }
        return Task.FromResult(true); // Default: enabled
    }
}
