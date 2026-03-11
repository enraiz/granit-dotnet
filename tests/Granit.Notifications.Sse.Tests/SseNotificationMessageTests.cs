using System.Text.Json;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Sse.Tests;

public sealed class SseNotificationMessageTests
{
    [Fact]
    public void Record_HasCorrectDefaults()
    {
        SseNotificationMessage message = new();

        message.NotificationId.ShouldBe(Guid.Empty);
        message.NotificationTypeName.ShouldBe(string.Empty);
        message.Severity.ShouldBe(NotificationSeverity.Info);
        message.RelatedEntityType.ShouldBeNull();
        message.RelatedEntityId.ShouldBeNull();
    }

    [Fact]
    public void Record_SupportsValueEquality()
    {
        JsonElement data = JsonSerializer.SerializeToElement(new { key = "value" });
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid id = Guid.NewGuid();

        SseNotificationMessage msg1 = new()
        {
            NotificationId = id,
            NotificationTypeName = "test",
            Severity = NotificationSeverity.Warning,
            Data = data,
            OccurredAt = now,
        };

        SseNotificationMessage msg2 = new()
        {
            NotificationId = id,
            NotificationTypeName = "test",
            Severity = NotificationSeverity.Warning,
            Data = data,
            OccurredAt = now,
        };

        msg1.ShouldBe(msg2);
    }
}
