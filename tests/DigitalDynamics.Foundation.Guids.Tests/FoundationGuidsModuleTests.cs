// =============================================================================
// Tests - FoundationGuidsModule
// =============================================================================
// Vérifie que le module enregistre les services Guids via ConfigureServices.
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DigitalDynamics.Foundation.Guids.Tests;

public sealed class FoundationGuidsModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersGuidGenerator()
    {
        // Arrange
        FoundationGuidsModule module = new();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(null);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        IGuidGenerator? generator = sp.GetService<IGuidGenerator>();
        generator.Should().NotBeNull();
        generator.Should().BeOfType<SequentialGuidGenerator>();
    }
}
