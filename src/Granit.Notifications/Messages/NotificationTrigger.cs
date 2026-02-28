using System.Text.Json;

namespace Granit.Notifications.Messages;

/// <summary>
/// Wolverine message published by <see cref="Abstractions.INotificationPublisher"/> to trigger notification fan-out.
/// </summary>
public sealed record NotificationTrigger
{
    public Guid NotificationId { get; init; } = Guid.NewGuid();
    public required string NotificationTypeName { get; init; }
    public required NotificationSeverity Severity { get; init; }
    public required JsonElement Data { get; init; }
    public IReadOnlyList<string> RecipientUserIds { get; init; } = [];
    public EntityReference? RelatedEntity { get; init; }
    public Guid? TenantId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public string? Culture { get; init; }
}
