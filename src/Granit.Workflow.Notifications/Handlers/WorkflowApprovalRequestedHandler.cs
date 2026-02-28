using Granit.Core.MultiTenancy;
using Granit.Notifications;
using Granit.Notifications.Abstractions;
using Granit.Workflow.Events;
using Microsoft.Extensions.Logging;

namespace Granit.Workflow.Notifications.Handlers;

/// <summary>
/// Wolverine handler that processes <see cref="WorkflowApprovalRequested"/> domain events
/// by notifying designated approvers via <see cref="INotificationPublisher"/>.
/// </summary>
/// <remarks>
/// <para>
/// When a user without the required permission triggers a transition that supports
/// approval routing (<c>RequiresApproval = true</c>), the workflow engine publishes
/// a <see cref="WorkflowApprovalRequested"/> event. This handler:
/// </para>
/// <list type="number">
///   <item>Resolves approver user IDs via <see cref="IApproverResolver"/>.</item>
///   <item>Sends a notification to each approver via <see cref="INotificationPublisher"/>.</item>
/// </list>
/// <para>
/// If no approvers are found, the handler logs a warning and returns without error.
/// This follows the graceful degradation pattern.
/// </para>
/// </remarks>
public sealed class WorkflowApprovalRequestedHandler(
    IApproverResolver approverResolver,
    INotificationPublisher notificationPublisher,
    ICurrentTenant currentTenant,
    ILogger<WorkflowApprovalRequestedHandler> logger)
{
    /// <summary>
    /// Handles the <see cref="WorkflowApprovalRequested"/> event by notifying approvers.
    /// </summary>
    public async Task HandleAsync(
        WorkflowApprovalRequested message,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> approverIds = await approverResolver.ResolveApproversAsync(
            message.RequiredPermission, cancellationToken);

        if (approverIds.Count == 0)
        {
            logger.LogWarning(
                "No approvers found for permission {Permission} on {EntityType} {EntityId}. " +
                "The approval request will not be delivered",
                message.RequiredPermission, message.EntityType, message.EntityId);
            return;
        }

        WorkflowApprovalNotificationData data = new(
            message.EntityType,
            message.EntityId,
            message.RequestedBy,
            message.TargetState,
            message.RequiredPermission);

        EntityReference relatedEntity = new(message.EntityType, message.EntityId);

        await notificationPublisher.PublishAsync(
            WorkflowApprovalNotificationType.Instance,
            data,
            approverIds,
            relatedEntity,
            cancellationToken);

        logger.LogInformation(
            "Approval notification sent to {ApproverCount} approvers for {EntityType} {EntityId} " +
            "(permission: {Permission}, tenant: {TenantId})",
            approverIds.Count,
            message.EntityType,
            message.EntityId,
            message.RequiredPermission,
            currentTenant.IsAvailable ? currentTenant.Id : (Guid?)null);
    }
}
