using Granit.Notifications.Abstractions;
using Granit.Notifications.Domain;

namespace Granit.Notifications.Internal;

/// <summary>
/// No-op delivery store for development. Replaced by EF Core store in production (ISO 27001 audit).
/// </summary>
internal sealed class NullNotificationDeliveryWriter : INotificationDeliveryWriter
{
    public Task RecordAsync(NotificationDeliveryAttempt attempt, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
