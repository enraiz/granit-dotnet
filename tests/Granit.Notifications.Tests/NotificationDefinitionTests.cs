// =============================================================================
// Tests - NotificationDefinition
// =============================================================================
// Verifies construction with name guard, default property values, and all
// init-only property assignments.
// =============================================================================

using Shouldly;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class NotificationDefinitionTests
{
    [Fact]
    public void Constructor_ValidName_SetsNameProperty()
    {
        NotificationDefinition definition = new("order.created");

        definition.Name.ShouldBe("order.created");
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentException() =>
        Should.Throw<ArgumentException>(() => new NotificationDefinition(null!));

    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentException() =>
        Should.Throw<ArgumentException>(() => new NotificationDefinition(""));

    [Fact]
    public void Constructor_WhitespaceName_ThrowsArgumentException() =>
        Should.Throw<ArgumentException>(() => new NotificationDefinition("   "));

    [Fact]
    public void DefaultSeverity_IsInfo()
    {
        NotificationDefinition definition = new("test.notification");

        definition.DefaultSeverity.ShouldBe(NotificationSeverity.Info);
    }

    [Fact]
    public void DefaultChannels_IsEmptyByDefault()
    {
        NotificationDefinition definition = new("test.notification");

        definition.DefaultChannels.ShouldBeEmpty();
    }

    [Fact]
    public void AllowUserOptOut_IsTrueByDefault()
    {
        NotificationDefinition definition = new("test.notification");

        definition.AllowUserOptOut.ShouldBeTrue();
    }

    [Fact]
    public void DisplayName_IsNullByDefault()
    {
        NotificationDefinition definition = new("test.notification");

        definition.DisplayName.ShouldBeNull();
    }

    [Fact]
    public void Description_IsNullByDefault()
    {
        NotificationDefinition definition = new("test.notification");

        definition.Description.ShouldBeNull();
    }

    [Fact]
    public void GroupName_IsNullByDefault()
    {
        NotificationDefinition definition = new("test.notification");

        definition.GroupName.ShouldBeNull();
    }

    [Fact]
    public void AllProperties_CanBeSetViaInitializers()
    {
        NotificationDefinition definition = new("security.alert")
        {
            DefaultSeverity = NotificationSeverity.Fatal,
            DefaultChannels = [NotificationChannels.InApp, NotificationChannels.Email, NotificationChannels.Sms],
            DisplayName = "Security Alert",
            Description = "Critical security event",
            GroupName = "Security",
            AllowUserOptOut = false,
        };

        definition.Name.ShouldBe("security.alert");
        definition.DefaultSeverity.ShouldBe(NotificationSeverity.Fatal);
        definition.DefaultChannels.Count.ShouldBe(3);
        definition.DisplayName.ShouldBe("Security Alert");
        definition.Description.ShouldBe("Critical security event");
        definition.GroupName.ShouldBe("Security");
        definition.AllowUserOptOut.ShouldBeFalse();
    }
}
