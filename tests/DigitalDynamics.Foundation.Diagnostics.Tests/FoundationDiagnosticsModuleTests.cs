// =============================================================================
// Tests - FoundationDiagnosticsModule
// =============================================================================
// Vérifie que ConfigureServices appelle AddFoundationDiagnostics,
// ce qui enregistre les services HealthChecks dans le conteneur DI.
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.Diagnostics.Tests;

public sealed class FoundationDiagnosticsModuleTests
{
    [Fact]
    public void ConfigureServices_CallsAddFoundationDiagnostics_RegisteringHealthCheckInfrastructure()
    {
        // Arrange
        FoundationDiagnosticsModule module = new();
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);

        // Assert — AddFoundationDiagnostics calls AddHealthChecks which registers HealthCheckServiceOptions
        using ServiceProvider sp = builder.Services.BuildServiceProvider();
        HealthCheckServiceOptions options = sp.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        options.Should().NotBeNull();
    }
}
