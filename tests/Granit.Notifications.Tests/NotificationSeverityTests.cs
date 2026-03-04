// =============================================================================
// Tests - NotificationSeverity
// =============================================================================
// Verifies enum member values and ordering. Guards against accidental changes
// to the integer representation used in database storage.
// =============================================================================

using Shouldly;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class NotificationSeverityTests
{
    [Theory]
    [InlineData(NotificationSeverity.Info, 0)]
    [InlineData(NotificationSeverity.Success, 1)]
    [InlineData(NotificationSeverity.Warning, 2)]
    [InlineData(NotificationSeverity.Error, 3)]
    [InlineData(NotificationSeverity.Fatal, 4)]
    public void EnumValue_HasExpectedIntegerRepresentation(NotificationSeverity severity, int expected) =>
        ((int)severity).ShouldBe(expected);

    [Fact]
    public void Enum_HasFiveMembers()
    {
        NotificationSeverity[] values = Enum.GetValues<NotificationSeverity>();

        values.Length.ShouldBe(5);
    }

    [Fact]
    public void Fatal_IsGreaterThan_Info()
    {
        (NotificationSeverity.Fatal > NotificationSeverity.Info).ShouldBeTrue();
    }
}
