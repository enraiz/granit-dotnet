// =============================================================================
// Tests - FoundationApiDocumentationModule
// =============================================================================
// Vérifie que ConfigureServices enregistre les services de documentation OpenAPI.
// =============================================================================

using DigitalDynamics.Foundation.ApiDocumentation.Options;
using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.ApiDocumentation.Tests;

public sealed class FoundationApiDocumentationModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersApiDocumentationOptions()
    {
        // Arrange
        FoundationApiDocumentationModule module = new();
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
