// =============================================================================
// Tests - GranitApiDocumentationModule
// =============================================================================
// Vérifie que ConfigureServices enregistre les services de documentation OpenAPI.
// =============================================================================

using FluentAssertions;
using Granit.ApiDocumentation.Options;
using Granit.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Granit.ApiDocumentation.Tests;

public sealed class GranitApiDocumentationModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersApiDocumentationOptions()
    {
        // Arrange
        GranitApiDocumentationModule module = new();
        HostApplicationBuilder builder = Host.CreateApplicationBuilder([]);
        ServiceConfigurationContext context = new(
            builder.Services,
            builder.Configuration,
            builder);

        // Act
        module.ConfigureServices(context);

        // Assert — ApiDocumentationOptions should be resolvable
        using Microsoft.Extensions.DependencyInjection.ServiceProvider sp =
            builder.Services.BuildServiceProvider();
        ApiDocumentationOptions options =
            sp.GetRequiredService<IOptions<ApiDocumentationOptions>>().Value;
        options.Should().NotBeNull();
        options.MajorVersions.Should().ContainSingle().Which.Should().Be(1, "default version is 1");
    }
}
