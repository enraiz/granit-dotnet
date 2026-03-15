using Granit.Notifications;

namespace Granit.Workflow.Notifications;

/// <summary>
/// Notification type for workflow state transitions.
/// Delivered to entity followers when a workflow changes state.
/// </summary>
public sealed class WorkflowStateChangedNotificationType
    : NotificationType<WorkflowStateChangedNotificationData>
{
    /// <summary>Singleton instance.</summary>
    public static readonly WorkflowStateChangedNotificationType Instance = new();

    /// <inheritdoc />
    public override string Name => "workflow.state_changed";

    /// <inheritdoc />
    public override NotificationSeverity DefaultSeverity => NotificationSeverity.Info;

    /// <inheritdoc />
    public override IReadOnlyList<string> DefaultChannels { get; } =
        [NotificationChannels.InApp, NotificationChannels.SignalR];
}

/// <summary>
/// Data payload for a workflow state change notification.
/// </summary>
/// <param name="EntityType">The entity type (e.g. "Publication").</param>
/// <param name="EntityId">The entity identifier.</param>
/// <param name="PreviousState">Previous state name.</param>
/// <param name="NewState">New state name.</param>
/// <param name="TransitionedBy">User ID who triggered the transition.</param>
public sealed record WorkflowStateChangedNotificationData(
    string EntityType,
    string EntityId,
    string PreviousState,
    string NewState,
    string TransitionedBy);
