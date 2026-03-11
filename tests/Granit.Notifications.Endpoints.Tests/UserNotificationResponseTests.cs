// =============================================================================
// Tests - UserNotificationResponse
// =============================================================================
// Vérifie que le record Response DTO expose les propriétés attendues
// et que l'égalité structurelle fonctionne.
// =============================================================================

using Granit.Notifications.Domain;
using Granit.Notifications.Endpoints.Dtos;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Endpoints.Tests;

public sealed class UserNotificationResponseTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;

        // Act
        UserNotificationResponse response = new(
            id, notificationId, "NewMessage", NotificationSeverity.Info,
            "user-1", null, UserNotificationState.Unread,
            createdAt, null, "Acme.Patients", "patient-42");

        // Assert
        response.Id.ShouldBe(id);
        response.NotificationId.ShouldBe(notificationId);
        response.NotificationTypeName.ShouldBe("NewMessage");
        response.Severity.ShouldBe(NotificationSeverity.Info);
        response.RecipientUserId.ShouldBe("user-1");
        response.Data.ShouldBeNull();
        response.State.ShouldBe(UserNotificationState.Unread);
        response.CreatedAt.ShouldBe(createdAt);
        response.ReadAt.ShouldBeNull();
        response.RelatedEntityType.ShouldBe("Acme.Patients");
        response.RelatedEntityId.ShouldBe("patient-42");
    }

    [Fact]
    public void Record_Equality_SameValues_AreEqual()
    {
        var id = Guid.NewGuid();
        var nid = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        UserNotificationResponse a = new(id, nid, "T", NotificationSeverity.Info, "u", null, UserNotificationState.Unread, now, null, null, null);
        UserNotificationResponse b = new(id, nid, "T", NotificationSeverity.Info, "u", null, UserNotificationState.Unread, now, null, null, null);

        a.ShouldBe(b);
    }
}
