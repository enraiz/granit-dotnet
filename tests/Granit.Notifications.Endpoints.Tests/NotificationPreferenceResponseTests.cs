// =============================================================================
// Tests - NotificationPreferenceResponse
// =============================================================================
// Vérifie que le record Response DTO expose les propriétés attendues.
// =============================================================================

using Shouldly;
using Xunit;

namespace Granit.Notifications.Endpoints.Tests;

public sealed class NotificationPreferenceResponseTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        // Act
        NotificationPreferenceResponse response = new(id, "user-1", "NewMessage", "Email", true);

        // Assert
        response.Id.ShouldBe(id);
        response.UserId.ShouldBe("user-1");
        response.NotificationTypeName.ShouldBe("NewMessage");
        response.ChannelName.ShouldBe("Email");
        response.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Record_Equality_SameValues_AreEqual()
    {
        Guid id = Guid.NewGuid();
        new NotificationPreferenceResponse(id, "u", "T", "C", true)
            .ShouldBe(new NotificationPreferenceResponse(id, "u", "T", "C", true));
    }
}
