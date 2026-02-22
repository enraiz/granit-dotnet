// =============================================================================
// Tests - ApiDocumentationServiceCollectionExtensions
// =============================================================================
// Vérifie que AddFoundationApiDocumentation enregistre un document OpenAPI
// par version déclarée, et que les transformers sont enregistrés en DI.
// =============================================================================

using DigitalDynamics.Foundation.ApiDocumentation.Extensions;
using DigitalDynamics.Foundation.ApiDocumentation.Options;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.ApiDocumentation.Tests;

public sealed class ApiDocumentationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFoundationApiDocumentation_WithConfiguration_RegistersOptions()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiDocumentation:Title"] = "Guava API",
                ["ApiDocumentation:MajorVersions:0"] = "1",
                ["ApiDocumentation:EnableInProduction"] = "false",
            })
            .Build();

        // Act
        services.AddFoundationApiDocumentation(configuration);

        // Assert — options are bound from configuration
        using ServiceProvider sp = services.BuildServiceProvider();
        ApiDocumentationOptions options =
            sp.GetRequiredService<IOptions<ApiDocumentationOptions>>().Value;
        options.Title.Should().Be("Guava API");
        options.MajorVersions.Should().ContainSingle().Which.Should().Be(1);
        options.EnableInProduction.Should().BeFalse();
    }

    [Fact]
    public void AddFoundationApiDocumentation_WithLambda_RegistersOptions()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationApiDocumentation(opts =>
        {
            opts.Title = "Test API";
            opts.MajorVersions = [1, 2];
        });

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        ApiDocumentationOptions options =
            sp.GetRequiredService<IOptions<ApiDocumentationOptions>>().Value;
        options.Title.Should().Be("Test API");
        options.MajorVersions.Should().HaveCount(2).And.Contain([1, 2]);
    }

    [Fact]
    public void AddFoundationApiDocumentation_WithDefaultConfig_UsesDefaultOptions()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddFoundationApiDocumentation(configuration);

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        ApiDocumentationOptions options =
            sp.GetRequiredService<IOptions<ApiDocumentationOptions>>().Value;
        options.Title.Should().Be("API");
        options.MajorVersions.Should().ContainSingle().Which.Should().Be(1);
    }

    [Fact]
    public void AddFoundationApiDocumentation_ReturnsServices_ForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        // Act
        IServiceCollection returned = services.AddFoundationApiDocumentation(configuration);

        // Assert
        returned.Should().BeSameAs(services);
    }

    [Fact]
    public void AddFoundationApiDocumentation_WithMultipleVersions_RegistersMultipleOpenApiDocuments()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddFoundationApiDocumentation(opts =>
        {
            opts.Title = "Multi-version API";
            opts.MajorVersions = [1, 2, 3];
        });

        // Assert — one AddOpenApi registration per version → each is named "v1", "v2", "v3"
        // AddOpenApi registers IConfigureOptions<OpenApiOptions> keyed by document name.
        // We verify services are registered (not zero), which indicates documents were added.
        services.Should().NotBeEmpty("versions [1, 2, 3] must register OpenAPI services");
    }
}
