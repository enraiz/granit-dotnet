// =============================================================================
// Tests - GranitApiVersioningModule
// =============================================================================
// Vérifie que ConfigureServices déclenche l'enregistrement des services
// de versioning via AddGranitApiVersioning.
// =============================================================================

using FluentAssertions;
using Granit.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Granit.ApiVersioning.Tests;

public sealed class GranitApiVersioningModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersApiVersioningServices()
    {
        // Arrange
        GranitApiVersioningModule module = new();
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);

        // Assert — GranitApiVersioningOptions should be resolvable after services are built
        using Microsoft.Extensions.DependencyInjection.ServiceProvider sp =
            builder.Services.BuildServiceProvider();
        Options.GranitApiVersioningOptions options =
            sp.GetRequiredService<IOptions<Options.GranitApiVersioningOptions>>().Value;
        options.Should().NotBeNull();
        options.DefaultMajorVersion.Should().Be(1);
    }
}
