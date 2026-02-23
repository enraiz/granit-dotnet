// =============================================================================
// Tests - CurrentTimezoneProvider
// =============================================================================
// Verifies that CurrentTimezoneProvider:
//   - Stores and returns the timezone correctly
//   - Isolates values between async contexts (AsyncLocal)
//   - Returns null by default
// =============================================================================

using FluentAssertions;
using Xunit;

namespace Granit.Timing.Tests;

public sealed class CurrentTimezoneProviderTests
{
    [Fact]
    public void Timezone_DefaultsToNull()
    {
        // Arrange
        var provider = new CurrentTimezoneProvider();

        // Assert
        provider.Timezone.Should().BeNull();
    }

    [Fact]
    public void Timezone_CanBeSetAndRead()
    {
        // Arrange
        var provider = new CurrentTimezoneProvider
        {
            // Act
            Timezone = "Europe/Brussels"
        };

        // Assert
        provider.Timezone.Should().Be("Europe/Brussels");
    }

    [Fact]
    public void Timezone_CanBeResetToNull()
    {
        // Arrange
        var provider = new CurrentTimezoneProvider
        {
            Timezone = "Europe/Brussels"
        };

        // Act
        provider.Timezone = null;

        // Assert
        provider.Timezone.Should().BeNull();
    }

    [Fact]
    public async Task Timezone_IsIsolatedPerAsyncContext()
    {
        // Arrange
        var provider = new CurrentTimezoneProvider
        {
            Timezone = "Europe/Brussels"
        };

        string? innerTimezone = null;

        // Act - launch a new async context
        await Task.Run(() =>
        {
            // The new async context inherits the parent value
            innerTimezone = provider.Timezone;
            // Modify within the child context
            provider.Timezone = "America/New_York";
        }, TestContext.Current.CancellationToken);

        // Assert - the value in the parent context is not affected
        provider.Timezone.Should().Be("Europe/Brussels",
            "AsyncLocal isolates modifications from the child context");
    }
}
