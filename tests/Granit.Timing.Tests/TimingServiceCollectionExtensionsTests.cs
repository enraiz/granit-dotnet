// =============================================================================
// Tests - TimingServiceCollectionExtensions
// =============================================================================
// Vérifie que AddGranitTiming enregistre les services attendus.
// =============================================================================

using Granit.Timing.Extensions;
using Granit.Timing.Options;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Granit.Timing.Tests;

public sealed class TimingServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGranitTiming_RegistersClock()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitTiming();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        IClock? clock = sp.GetService<IClock>();
        clock.ShouldNotBeNull();
        clock.ShouldBeOfType<Clock>();
    }

    [Fact]
    public void AddGranitTiming_RegistersTimezoneProvider()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitTiming();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        ICurrentTimezoneProvider? provider = sp.GetService<ICurrentTimezoneProvider>();
        provider.ShouldNotBeNull();
        provider.ShouldBeOfType<CurrentTimezoneProvider>();
    }

    [Fact]
    public void AddGranitTiming_RegistersTimeProvider()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitTiming();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        TimeProvider? timeProvider = sp.GetService<TimeProvider>();
        timeProvider.ShouldNotBeNull();
        timeProvider.ShouldBeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddGranitTiming_TryAddSingleton_DoesNotOverrideExisting()
    {
        // Arrange
        ServiceCollection services = new();
        IClock customClock = NSubstitute.Substitute.For<IClock>();
        services.AddSingleton(customClock);

        // Act
        services.AddGranitTiming();

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        IClock resolved = sp.GetRequiredService<IClock>();
        resolved.ShouldBeSameAs(customClock);
    }

    [Fact]
    public void AddGranitTiming_WithConfigure_AppliesOptions()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddGranitTiming(opts =>
        {
            opts.DefaultTimezone = "America/New_York";
        });

        using ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        ClockOptions options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ClockOptions>>().Value;
        options.DefaultTimezone.ShouldBe("America/New_York");
    }
}
