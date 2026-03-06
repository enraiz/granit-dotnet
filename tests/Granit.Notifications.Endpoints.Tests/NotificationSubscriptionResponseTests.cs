// =============================================================================
// Tests - NotificationSubscriptionResponse
// =============================================================================
// Vérifie que le record Response DTO expose les propriétés attendues.
// =============================================================================

using Shouldly;
using Xunit;

namespace Granit.Notifications.Endpoints.Tests;

public sealed class NotificationSubscriptionResponseTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        // Act
        NotificationSubscriptionResponse response = new(id, "user-1", "NewMessage", "Guava.Patients", "p-42");

        // Assert
        response.Id.ShouldBe(id);
        response.UserId.ShouldBe("user-1");
        response.NotificationTypeName.ShouldBe("NewMessage");
        response.EntityType.ShouldBe("Guava.Patients");
        response.EntityId.ShouldBe("p-42");
    }

    [Fact]
    public void Constructor_NullOptionalFields()
    {
        Guid id = Guid.NewGuid();
        NotificationSubscriptionResponse response = new(id, "user-1", "TopicSub", null, null);

        response.EntityType.ShouldBeNull();
        response.EntityId.ShouldBeNull();
    }
}
