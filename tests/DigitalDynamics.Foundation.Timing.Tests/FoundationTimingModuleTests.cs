// =============================================================================
// Tests - FoundationTimingModule
// =============================================================================
// Vérifie que le module enregistre les services Timing via ConfigureServices.
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DigitalDynamics.Foundation.Timing.Tests;

public sealed class FoundationTimingModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersTimingServices()
    {
        // Arrange
        FoundationTimingModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        IClock? clock = sp.GetService<IClock>();
        clock.Should().NotBeNull();

        ICurrentTimezoneProvider? tzProvider = sp.GetService<ICurrentTimezoneProvider>();
        tzProvider.Should().NotBeNull();

        TimeProvider? timeProvider = sp.GetService<TimeProvider>();
        timeProvider.Should().NotBeNull();
    }
}
