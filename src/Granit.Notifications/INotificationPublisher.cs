namespace Granit.Notifications;

/// <summary>
/// Facade for publishing notifications to users. The implementation fans out
/// the notification to registered channels (InApp, Email, SMS, Push, etc.)
/// via Wolverine Outbox.
/// </summary>
/// <remarks>
/// <para>
/// Two delivery modes:
/// <list type="bullet">
///   <item>Explicit recipients: <c>PublishAsync</c> with a list of user IDs.</item>
///   <item>Entity followers: <c>PublishToEntityFollowersAsync</c> (Odoo-style).</item>
/// </list>
/// </para>
/// <para>
/// The default implementation (<c>WolverineNotificationPublisher</c>) converts the call
/// into a <c>NotificationTrigger</c> message processed via the Wolverine Outbox.
/// </para>
/// </remarks>
public interface INotificationPublisher
{
    /// <summary>
    /// Publishes a notification to the specified recipients.
    /// </summary>
    /// <param name="notificationTypeName">Logical notification type name (e.g. "workflow.approval_requested").</param>
    /// <param name="title">Short notification title.</param>
    /// <param name="body">Notification body (supports markdown).</param>
    /// <param name="recipientUserIds">User IDs to notify.</param>
    /// <param name="relatedEntity">Optional related entity reference for linking.</param>
    /// <param name="severity">Notification severity level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask PublishAsync(
        string notificationTypeName,
        string title,
        string body,
        IReadOnlyList<string> recipientUserIds,
        EntityReference? relatedEntity = null,
        NotificationSeverity severity = NotificationSeverity.Info,
        CancellationToken cancellationToken = default);
}
