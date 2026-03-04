using System.Text.Json;
using Granit.Core.Domain;
using Granit.Notifications.Domain;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Tests.Domain;

public sealed class UserNotificationTests
{
    [Fact]
    public void InheritsEntity() =>
        typeof(UserNotification).IsAssignableTo(typeof(Entity)).ShouldBeTrue();

    [Fact]
    public void ImplementsIMultiTenant() =>
        typeof(UserNotification).IsAssignableTo(typeof(IMultiTenant)).ShouldBeTrue();

    [Fact]
    public void IsSealed() =>
        typeof(UserNotification).IsSealed.ShouldBeTrue();

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        UserNotification notification = new();

        notification.Id.ShouldBe(Guid.Empty);
        notification.NotificationId.ShouldBe(Guid.Empty);
        notification.NotificationTypeName.ShouldBe(string.Empty);
        notification.Severity.ShouldBe(NotificationSeverity.Info);
        notification.RecipientUserId.ShouldBe(string.Empty);
        notification.State.ShouldBe(UserNotificationState.Unread);
        notification.CreatedAt.ShouldBe(default);
        notification.ReadAt.ShouldBeNull();
        notification.TenantId.ShouldBeNull();
        notification.RelatedEntityType.ShouldBeNull();
        notification.RelatedEntityId.ShouldBeNull();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        Guid notificationId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        JsonElement data = JsonDocument.Parse("""{"key":"value"}""").RootElement;

        UserNotification notification = new()
        {
            NotificationId = notificationId,
            NotificationTypeName = "order.created",
            Severity = NotificationSeverity.Warning,
            RecipientUserId = "user-42",
            Data = data,
            State = UserNotificationState.Read,
            CreatedAt = now,
            ReadAt = now.AddMinutes(5),
            TenantId = tenantId,
            RelatedEntityType = "Order",
            RelatedEntityId = "ORD-001",
        };

        notification.NotificationId.ShouldBe(notificationId);
        notification.NotificationTypeName.ShouldBe("order.created");
        notification.Severity.ShouldBe(NotificationSeverity.Warning);
        notification.RecipientUserId.ShouldBe("user-42");
        notification.Data.GetProperty("key").GetString().ShouldBe("value");
        notification.State.ShouldBe(UserNotificationState.Read);
        notification.CreatedAt.ShouldBe(now);
        notification.ReadAt.ShouldBe(now.AddMinutes(5));
        notification.TenantId.ShouldBe(tenantId);
        notification.RelatedEntityType.ShouldBe("Order");
        notification.RelatedEntityId.ShouldBe("ORD-001");
    }

    [Fact]
    public void DefaultState_IsUnread() =>
        new UserNotification().State.ShouldBe(UserNotificationState.Unread);
}
