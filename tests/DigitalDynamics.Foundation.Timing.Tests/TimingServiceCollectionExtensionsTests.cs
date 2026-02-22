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
        ServiceCollection services = new();

        // Act
        services.AddFoundationTiming();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        IClock? clock = sp.GetService<IClock>();
        clock.Should().NotBeNull();
        clock.Should().BeOfType<Clock>();
    }

    [Fact]
    public void AddFoundationTiming_RegistersTimezoneProvider()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationTiming();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        ICurrentTimezoneProvider? provider = sp.GetService<ICurrentTimezoneProvider>();
        provider.Should().NotBeNull();
        provider.Should().BeOfType<CurrentTimezoneProvider>();
    }

    [Fact]
    public void AddFoundationTiming_RegistersTimeProvider()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationTiming();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        TimeProvider? timeProvider = sp.GetService<TimeProvider>();
        timeProvider.Should().NotBeNull();
        timeProvider.Should().BeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddFoundationTiming_TryAddSingleton_DoesNotOverrideExisting()
    {
        // Arrange
        ServiceCollection services = new();
        IClock customClock = NSubstitute.Substitute.For<IClock>();
        services.AddSingleton(customClock);

        // Act
        services.AddFoundationTiming();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        IClock resolved = sp.GetRequiredService<IClock>();
        resolved.Should().BeSameAs(customClock);
    }

    [Fact]
    public void AddFoundationTiming_WithConfigure_AppliesOptions()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationTiming(opts =>
        {
            opts.DefaultTimezone = "America/New_York";
        });

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        ClockOptions options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ClockOptions>>().Value;
        options.DefaultTimezone.Should().Be("America/New_York");
    }
}
