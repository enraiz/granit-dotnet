// =============================================================================
// Tests - FoundationObservabilityModule
// =============================================================================
// Vérifie que le module enregistre les services Observability via ConfigureServices.
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Observability.Options;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.Observability.Tests;

public sealed class FoundationObservabilityModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersObservabilityOptions()
    {
        // Arrange
        FoundationObservabilityModule module = new();
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);

        using ServiceProvider sp = builder.Services.BuildServiceProvider();

        // Assert
        ObservabilityOptions options = sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value;
        options.Should().NotBeNull();
        options.ServiceName.Should().NotBeNullOrEmpty();
    }
}
