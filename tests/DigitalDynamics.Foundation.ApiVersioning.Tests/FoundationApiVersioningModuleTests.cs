// =============================================================================
// Tests - FoundationApiVersioningModule
// =============================================================================
// Vérifie que ConfigureServices déclenche l'enregistrement des services
// de versioning via AddFoundationApiVersioning.
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.ApiVersioning.Tests;

public sealed class FoundationApiVersioningModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersApiVersioningServices()
    {
        // Arrange
        FoundationApiVersioningModule module = new();
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);

        // Assert — FoundationApiVersioningOptions should be resolvable after services are built
        using Microsoft.Extensions.DependencyInjection.ServiceProvider sp =
            builder.Services.BuildServiceProvider();
        Options.FoundationApiVersioningOptions options =
            sp.GetRequiredService<IOptions<Options.FoundationApiVersioningOptions>>().Value;
        options.Should().NotBeNull();
        options.DefaultMajorVersion.Should().Be(1);
    }
}
