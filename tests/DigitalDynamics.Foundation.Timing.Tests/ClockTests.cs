// =============================================================================
// Tests - Clock
// =============================================================================
// Verifies that the Clock implementation:
//   - Returns UTC time via TimeProvider
//   - Normalizes local DateTimeOffset values to UTC
//   - Converts to the user's timezone (ConvertToUserTime)
//   - Converts to UTC (ConvertToUtc)
//   - Works without a configured timezone (returns the value unchanged)
// =============================================================================

using DigitalDynamics.Foundation.Timing;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace DigitalDynamics.Foundation.Timing.Tests;

public sealed class ClockTests
{
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly ICurrentTimezoneProvider _timezoneProvider;
    private readonly Clock _clock;

    public ClockTests()
    {
        _fakeTimeProvider = new FakeTimeProvider();
        _timezoneProvider = Substitute.For<ICurrentTimezoneProvider>();
        _clock = new Clock(_fakeTimeProvider, _timezoneProvider);
    }

    [Fact]
    public void Now_ReturnsUtcFromTimeProvider()
    {
        // Arrange
        DateTimeOffset expected = new(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
        _fakeTimeProvider.SetUtcNow(expected);

        // Act
        DateTimeOffset now = _clock.Now;

        // Assert
        now.Should().Be(expected);
        now.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Now_IsAlwaysUtc()
    {
        // Act
        DateTimeOffset now = _clock.Now;

        // Assert
        now.Offset.Should().Be(TimeSpan.Zero, "Clock must always return UTC (HDS compliance)");
    }

    [Fact]
    public void SupportsMultipleTimezone_ReturnsTrue() => _clock.SupportsMultipleTimezone.Should().BeTrue();

    [Fact]
    public void Normalize_ConvertsLocalOffsetToUtc()
    {
<<<<<<< HEAD
        // Arrange - DateTimeOffset with offset +02:00 (Europe/Brussels in summer)
        var localTime = new DateTimeOffset(2026, 6, 15, 14, 30, 0, TimeSpan.FromHours(2));
=======
        // Arrange - DateTimeOffset avec offset +02:00 (Europe/Brussels en ete)
        DateTimeOffset localTime = new(2026, 6, 15, 14, 30, 0, TimeSpan.FromHours(2));
>>>>>>> feature/settings-module

        // Act
        DateTimeOffset normalized = _clock.Normalize(localTime);

        // Assert - Must be converted to UTC (+00:00), same instant
        normalized.Offset.Should().Be(TimeSpan.Zero);
        normalized.Should().Be(new DateTimeOffset(2026, 6, 15, 12, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Normalize_KeepsUtcUnchanged()
    {
        // Arrange
        DateTimeOffset utcTime = new(2026, 6, 15, 12, 30, 0, TimeSpan.Zero);

        // Act
        DateTimeOffset normalized = _clock.Normalize(utcTime);

        // Assert
        normalized.Should().Be(utcTime);
    }

    [Fact]
    public void Normalize_ConvertsNegativeOffsetToUtc()
    {
<<<<<<< HEAD
        // Arrange - DateTimeOffset with offset -05:00 (America/New_York)
        var localTime = new DateTimeOffset(2026, 6, 15, 7, 30, 0, TimeSpan.FromHours(-5));
=======
        // Arrange - DateTimeOffset avec offset -05:00 (America/New_York)
        DateTimeOffset localTime = new(2026, 6, 15, 7, 30, 0, TimeSpan.FromHours(-5));
>>>>>>> feature/settings-module

        // Act
        DateTimeOffset normalized = _clock.Normalize(localTime);

        // Assert
        normalized.Offset.Should().Be(TimeSpan.Zero);
        normalized.Should().Be(new DateTimeOffset(2026, 6, 15, 12, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ConvertToUserTime_WithTimezone_ConvertsCorrectly()
    {
        // Arrange
        _timezoneProvider.Timezone.Returns("Europe/Brussels");
        DateTimeOffset utcTime = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        TimeZoneInfo tzInfo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");
        TimeSpan expectedOffset = tzInfo.GetUtcOffset(utcTime);

        // Act
        DateTimeOffset userTime = _clock.ConvertToUserTime(utcTime);

<<<<<<< HEAD
        // Assert - Brussels is UTC+2 in summer (CEST)
        userTime.Offset.Should().Be(TimeSpan.FromHours(2));
        userTime.LocalDateTime.Hour.Should().Be(14);
=======
        // Assert - le meme instant UTC, converti dans le fuseau utilisateur
        userTime.UtcDateTime.Should().Be(utcTime.UtcDateTime, "la conversion ne doit pas changer l'instant UTC");
        userTime.Offset.Should().Be(expectedOffset);
>>>>>>> feature/settings-module
    }

    [Fact]
    public void ConvertToUserTime_WithoutTimezone_ReturnsUnchanged()
    {
        // Arrange
        _timezoneProvider.Timezone.Returns((string?)null);
        DateTimeOffset utcTime = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

        // Act
        DateTimeOffset userTime = _clock.ConvertToUserTime(utcTime);

        // Assert
        userTime.Should().Be(utcTime);
    }

    [Fact]
    public void ConvertToUserTime_WithEmptyTimezone_ReturnsUnchanged()
    {
        // Arrange
        _timezoneProvider.Timezone.Returns("");
        DateTimeOffset utcTime = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

        // Act
        DateTimeOffset userTime = _clock.ConvertToUserTime(utcTime);

        // Assert
        userTime.Should().Be(utcTime);
    }

    [Fact]
    public void ConvertToUtc_ConvertsToUtc()
    {
<<<<<<< HEAD
        // Arrange - DateTimeOffset with offset +02:00
        var localTime = new DateTimeOffset(2026, 6, 15, 14, 0, 0, TimeSpan.FromHours(2));
=======
        // Arrange - DateTimeOffset avec offset +02:00
        DateTimeOffset localTime = new(2026, 6, 15, 14, 0, 0, TimeSpan.FromHours(2));
>>>>>>> feature/settings-module

        // Act
        DateTimeOffset utcTime = _clock.ConvertToUtc(localTime);

        // Assert
        utcTime.Offset.Should().Be(TimeSpan.Zero);
        utcTime.Should().Be(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ConvertToUtc_KeepsUtcUnchanged()
    {
        // Arrange
        DateTimeOffset utcTime = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

        // Act
        DateTimeOffset result = _clock.ConvertToUtc(utcTime);

        // Assert
        result.Should().Be(utcTime);
    }
}
