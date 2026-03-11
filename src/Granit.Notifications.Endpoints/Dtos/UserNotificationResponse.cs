using Granit.Notifications.Domain;

namespace Granit.Notifications.Endpoints.Dtos;

/// <summary>
/// Response DTO for a user notification inbox entry.
/// </summary>
public sealed record UserNotificationResponse(
    Guid Id,
    Guid NotificationId,
    string NotificationTypeName,
    NotificationSeverity Severity,
    string RecipientUserId,
    object? Data,
    UserNotificationState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    string? RelatedEntityType,
    string? RelatedEntityId);
