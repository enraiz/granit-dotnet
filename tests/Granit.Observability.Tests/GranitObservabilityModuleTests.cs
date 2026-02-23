// =============================================================================
// Tests - GranitObservabilityModule
// =============================================================================
// Vérifie que le module enregistre les services Observability via ConfigureServices.
// =============================================================================

using FluentAssertions;
using Granit.Core.Modularity;
using Granit.Observability.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Granit.Observability.Tests;

public sealed class GranitObservabilityModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersObservabilityOptions()
    {
        // Arrange
        GranitObservabilityModule module = new();
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
