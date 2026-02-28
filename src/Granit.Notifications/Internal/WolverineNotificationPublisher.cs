using System.Text.Json;
using Granit.Core.MultiTenancy;
using Granit.Notifications.Abstractions;
using Granit.Notifications.Messages;
using Granit.Timing;
using Wolverine;

namespace Granit.Notifications.Internal;

/// <summary>
/// <see cref="INotificationPublisher"/> implementation that publishes
/// <see cref="NotificationTrigger"/> messages into the Wolverine Outbox.
/// </summary>
internal sealed class WolverineNotificationPublisher(
    IMessageBus messageBus,
    ICurrentTenant currentTenant,
    IClock clock) : INotificationPublisher
{
    public ValueTask PublishAsync<TData>(
        NotificationType<TData> notificationType,
        TData data,
        IReadOnlyList<string> recipientUserIds,
        CancellationToken ct = default) where TData : notnull =>
        PublishAsync(notificationType, data, recipientUserIds, relatedEntity: null, ct);

    public async ValueTask PublishAsync<TData>(
        NotificationType<TData> notificationType,
        TData data,
        IReadOnlyList<string> recipientUserIds,
        EntityReference? relatedEntity,
        CancellationToken ct = default) where TData : notnull
    {
        NotificationTrigger trigger = BuildTrigger(notificationType, data, relatedEntity);
        trigger = trigger with { RecipientUserIds = recipientUserIds };
        await messageBus.PublishAsync(trigger);
    }

    public async ValueTask PublishToSubscribersAsync<TData>(
        NotificationType<TData> notificationType,
        TData data,
        CancellationToken ct = default) where TData : notnull
    {
        NotificationTrigger trigger = BuildTrigger(notificationType, data, relatedEntity: null);
        await messageBus.PublishAsync(trigger);
    }

    public async ValueTask PublishToEntityFollowersAsync<TData>(
        NotificationType<TData> notificationType,
        TData data,
        EntityReference relatedEntity,
        CancellationToken ct = default) where TData : notnull
    {
        NotificationTrigger trigger = BuildTrigger(notificationType, data, relatedEntity);
        await messageBus.PublishAsync(trigger);
    }

    private NotificationTrigger BuildTrigger<TData>(
        NotificationType<TData> notificationType,
        TData data,
        EntityReference? relatedEntity) where TData : notnull => new()
        {
            NotificationTypeName = notificationType.Name,
            Severity = notificationType.DefaultSeverity,
            Data = JsonSerializer.SerializeToElement(data),
            RelatedEntity = relatedEntity,
            TenantId = currentTenant.IsAvailable ? currentTenant.Id : null,
            OccurredAt = clock.Now,
        };
}
