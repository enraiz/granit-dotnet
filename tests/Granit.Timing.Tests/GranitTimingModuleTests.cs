// =============================================================================
// Tests - GranitTimingModule
// =============================================================================
// Vérifie que le module enregistre les services Timing via ConfigureServices.
// =============================================================================

using FluentAssertions;
using Granit.Core.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Granit.Timing.Tests;

public sealed class GranitTimingModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersTimingServices()
    {
        // Arrange
        GranitTimingModule module = new();
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
