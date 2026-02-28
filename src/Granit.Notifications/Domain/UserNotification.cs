using Granit.Core.Domain;

namespace Granit.Notifications.Domain;

/// <summary>
/// In-app notification stored in the user's inbox.
/// The database is the source of truth; email/SMS/push are copies (Django lesson).
/// </summary>
public sealed class UserNotification : Entity, IMultiTenant
{
    public Guid NotificationId { get; set; }
    public string NotificationTypeName { get; set; } = string.Empty;
    public NotificationSeverity Severity { get; set; }
    public string RecipientUserId { get; set; } = string.Empty;
    public System.Text.Json.JsonElement Data { get; set; }
    public UserNotificationState State { get; set; } = UserNotificationState.Unread;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public Guid? TenantId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
}
