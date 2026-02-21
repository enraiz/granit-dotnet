// =============================================================================
// Tests - TimingServiceCollectionExtensions
// =============================================================================
// Vérifie que AddFoundationTiming enregistre les services attendus.
// =============================================================================

using DigitalDynamics.Foundation.Timing.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DigitalDynamics.Foundation.Timing.Tests;

public sealed class TimingServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFoundationTiming_RegistersClock()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFoundationTiming();

        using var sp = services.BuildServiceProvider();

        // Assert
        var clock = sp.GetService<IClock>();
        clock.Should().NotBeNull();
        clock.Should().BeOfType<Clock>();
    }

    [Fact]
    public void AddFoundationTiming_RegistersTimezoneProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFoundationTiming();

        using var sp = services.BuildServiceProvider();

        // Assert
        var provider = sp.GetService<ICurrentTimezoneProvider>();
        provider.Should().NotBeNull();
        provider.Should().BeOfType<CurrentTimezoneProvider>();
    }

    [Fact]
    public void AddFoundationTiming_RegistersTimeProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFoundationTiming();

        using var sp = services.BuildServiceProvider();

        // Assert
        var timeProvider = sp.GetService<TimeProvider>();
        timeProvider.Should().NotBeNull();
        timeProvider.Should().BeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddFoundationTiming_TryAddSingleton_DoesNotOverrideExisting()
    {
        // Arrange
        var services = new ServiceCollection();
        var customClock = NSubstitute.Substitute.For<IClock>();
        services.AddSingleton(customClock);

        // Act
        services.AddFoundationTiming();

        using var sp = services.BuildServiceProvider();

        // Assert
        var resolved = sp.GetRequiredService<IClock>();
        resolved.Should().BeSameAs(customClock);
    }

    [Fact]
    public void AddFoundationTiming_WithConfigure_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFoundationTiming(opts =>
        {
            opts.DefaultTimezone = "America/New_York";
        });

        using var sp = services.BuildServiceProvider();

        // Assert
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ClockOptions>>().Value;
        options.DefaultTimezone.Should().Be("America/New_York");
    }
}
