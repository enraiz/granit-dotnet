using Granit.Notifications;
using Granit.Notifications.Abstractions;
using Granit.Workflow.Events;
using Microsoft.Extensions.Logging;

namespace Granit.Workflow.Notifications.Handlers;

/// <summary>
/// Wolverine handler that processes <see cref="WorkflowStateChangedEvent"/> domain events
/// by notifying entity followers via <see cref="INotificationPublisher"/>.
/// </summary>
public sealed class WorkflowStateChangedHandler(
    INotificationPublisher notificationPublisher,
    ILogger<WorkflowStateChangedHandler> logger)
{
    /// <summary>
    /// Handles the <see cref="WorkflowStateChangedEvent"/> by notifying entity followers.
    /// </summary>
    public async Task HandleAsync(
        WorkflowStateChangedEvent message,
        CancellationToken cancellationToken)
    {
        WorkflowStateChangedNotificationData data = new(
            message.EntityType,
            message.EntityId,
            message.PreviousState,
            message.NewState,
            message.TransitionedBy);

        EntityReference relatedEntity = new(message.EntityType, message.EntityId);

        await notificationPublisher.PublishToEntityFollowersAsync(
            WorkflowStateChangedNotificationType.Instance,
            data,
            relatedEntity,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Workflow state change notification sent for {EntityType} {EntityId}: {PreviousState} -> {NewState}",
            message.EntityType,
            message.EntityId,
            message.PreviousState,
            message.NewState);
    }
}
