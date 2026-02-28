using Granit.Notifications;

namespace Granit.Workflow.Notifications;

/// <summary>
/// Notification type for workflow approval requests.
/// </summary>
public sealed class WorkflowApprovalNotificationType
    : NotificationType<WorkflowApprovalNotificationData>
{
    /// <summary>Singleton instance.</summary>
    public static readonly WorkflowApprovalNotificationType Instance = new();

    /// <inheritdoc />
    public override string Name => "workflow.approval_requested";

    /// <inheritdoc />
    public override NotificationSeverity DefaultSeverity => NotificationSeverity.Warning;

    /// <inheritdoc />
    public override IReadOnlyList<string> DefaultChannels { get; } =
        [NotificationChannels.InApp, NotificationChannels.Email];
}

/// <summary>
/// Data payload for a workflow approval notification.
/// </summary>
/// <param name="EntityType">The entity type requiring approval.</param>
/// <param name="EntityId">The entity identifier.</param>
/// <param name="RequestedBy">The user who requested the transition.</param>
/// <param name="TargetState">The desired target state.</param>
/// <param name="RequiredPermission">The permission required to approve.</param>
public sealed record WorkflowApprovalNotificationData(
    string EntityType,
    string EntityId,
    string RequestedBy,
    string TargetState,
    string RequiredPermission);
