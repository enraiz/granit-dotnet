using System.Text.Json;
using Shouldly;
using Xunit;

namespace Granit.Notifications.SignalR.Tests;

public sealed class SignalRNotificationMessageTests
{
    [Fact]
    public void Default_NotificationId_IsEmptyGuid() =>
        new SignalRNotificationMessage().NotificationId.ShouldBe(Guid.Empty);

    [Fact]
    public void Default_NotificationTypeName_IsEmpty() =>
        new SignalRNotificationMessage().NotificationTypeName.ShouldBe(string.Empty);

    [Fact]
    public void Default_Severity_IsInfo() =>
        new SignalRNotificationMessage().Severity.ShouldBe(NotificationSeverity.Info);

    [Fact]
    public void Default_RelatedEntityType_IsNull() =>
        new SignalRNotificationMessage().RelatedEntityType.ShouldBeNull();

    [Fact]
    public void Default_RelatedEntityId_IsNull() =>
        new SignalRNotificationMessage().RelatedEntityId.ShouldBeNull();

    [Fact]
    public void Properties_CanBeSet()
    {
        var id = Guid.NewGuid();
        DateTimeOffset occurredAt = DateTimeOffset.UtcNow;
        JsonElement data = JsonSerializer.SerializeToElement(new { foo = "bar" });

        SignalRNotificationMessage message = new()
        {
            NotificationId = id,
            NotificationTypeName = "test.type",
            Severity = NotificationSeverity.Warning,
            Data = data,
            RelatedEntityType = "Order",
            RelatedEntityId = "order-1",
            OccurredAt = occurredAt,
        };

        message.NotificationId.ShouldBe(id);
        message.NotificationTypeName.ShouldBe("test.type");
        message.Severity.ShouldBe(NotificationSeverity.Warning);
        message.Data.GetProperty("foo").GetString().ShouldBe("bar");
        message.RelatedEntityType.ShouldBe("Order");
        message.RelatedEntityId.ShouldBe("order-1");
        message.OccurredAt.ShouldBe(occurredAt);
    }

    [Fact]
    public void Record_SupportsEquality()
    {
        var id = Guid.NewGuid();
        DateTimeOffset occurredAt = DateTimeOffset.UtcNow;
        JsonElement data = JsonSerializer.SerializeToElement(new { key = "value" });

        SignalRNotificationMessage message1 = new()
        {
            NotificationId = id,
            NotificationTypeName = "type-a",
            Severity = NotificationSeverity.Error,
            Data = data,
            OccurredAt = occurredAt,
        };

        SignalRNotificationMessage message2 = new()
        {
            NotificationId = id,
            NotificationTypeName = "type-a",
            Severity = NotificationSeverity.Error,
            Data = data,
            OccurredAt = occurredAt,
        };

        // Records implement value equality
        message1.ShouldBe(message2);
    }

    [Fact]
    public void Record_IsSealed() =>
        typeof(SignalRNotificationMessage).IsSealed.ShouldBeTrue();
}
