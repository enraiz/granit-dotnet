using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;

namespace Granit.Notifications.Internal;

/// <summary>
/// No-op delivery store for development. Replaced by EF Core store in production (HDS audit).
/// </summary>
internal sealed class NullNotificationDeliveryStore : INotificationDeliveryStore
{
    public Task RecordAsync(NotificationDeliveryAttempt attempt, CancellationToken ct = default) =>
        Task.CompletedTask;
}
