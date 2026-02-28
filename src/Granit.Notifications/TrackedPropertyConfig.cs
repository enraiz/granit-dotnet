namespace Granit.Notifications;

/// <summary>
/// Configuration for an auto-tracked property on an <see cref="ITrackedEntity"/>.
/// </summary>
public sealed record TrackedPropertyConfig
{
    /// <summary>Name of the <see cref="NotificationType{TData}"/> to publish when this property changes.</summary>
    public required string NotificationTypeName { get; init; }

    /// <summary>Severity of the generated notification.</summary>
    public NotificationSeverity Severity { get; init; } = NotificationSeverity.Info;
}
