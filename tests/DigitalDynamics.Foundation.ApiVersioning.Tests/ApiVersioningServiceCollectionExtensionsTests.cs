// =============================================================================
// Tests - ApiVersioningServiceCollectionExtensions
// =============================================================================
// Vérifie que AddFoundationApiVersioning enregistre correctement les services
// Asp.Versioning et applique la configuration appsettings.
// =============================================================================

using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using DigitalDynamics.Foundation.ApiVersioning.Extensions;
using DigitalDynamics.Foundation.ApiVersioning.Options;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DigitalDynamics.Foundation.ApiVersioning.Tests;

public sealed class ApiVersioningServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFoundationApiVersioning_RegistersApiVersioningServices()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddFoundationApiVersioning(configuration);

        // Assert — IApiVersionReader, IApiVersionSelector, etc. should be registered
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType.Name.Contains("ApiVersion", StringComparison.OrdinalIgnoreCase));
        descriptor.Should().NotBeNull("AddApiVersioning must register versioning services");
    }

    [Fact]
    public void AddFoundationApiVersioning_WithCustomOptions_AppliesConfiguration()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiVersioning:DefaultMajorVersion"] = "2",
                ["ApiVersioning:ReportApiVersions"] = "false",
            })
            .Build();

        // Act
        services.AddFoundationApiVersioning(configuration);

        // Assert — options are bound from configuration
        using ServiceProvider sp = services.BuildServiceProvider();
        FoundationApiVersioningOptions options = sp.GetRequiredService<IOptions<FoundationApiVersioningOptions>>().Value;
        options.DefaultMajorVersion.Should().Be(2);
        options.ReportApiVersions.Should().BeFalse();
    }

    [Fact]
    public void AddFoundationApiVersioning_WithDefaultConfig_UsesDefaultOptions()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddFoundationApiVersioning(configuration);

        // Assert
        using ServiceProvider sp = services.BuildServiceProvider();
        FoundationApiVersioningOptions options = sp.GetRequiredService<IOptions<FoundationApiVersioningOptions>>().Value;
        options.DefaultMajorVersion.Should().Be(1);
        options.ReportApiVersions.Should().BeTrue();
    }

    [Fact]
    public void AddFoundationApiVersioning_ReturnsServices_ForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        // Act
        IServiceCollection returned = services.AddFoundationApiVersioning(configuration);

        // Assert
        returned.Should().BeSameAs(services);
    }

    [Fact]
    public void AddFoundationApiVersioning_ConfiguresApiVersioningOptions_WithDefaults()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddFoundationApiVersioning(configuration);

        // Act — resolving IOptions triggers the AddApiVersioning lambda
        using ServiceProvider sp = services.BuildServiceProvider();
        ApiVersioningOptions options = sp.GetRequiredService<IOptions<ApiVersioningOptions>>().Value;

        // Assert
        options.DefaultApiVersion.Should().Be(new ApiVersion(1));
        options.AssumeDefaultVersionWhenUnspecified.Should().BeTrue();
        options.ReportApiVersions.Should().BeTrue();
    }

    [Fact]
    public void AddFoundationApiVersioning_ConfiguresApiVersioningOptions_WithCustomConfig()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiVersioning:DefaultMajorVersion"] = "3",
                ["ApiVersioning:ReportApiVersions"] = "false",
            })
            .Build();
        services.AddFoundationApiVersioning(configuration);

        // Act
        using ServiceProvider sp = services.BuildServiceProvider();
        ApiVersioningOptions options = sp.GetRequiredService<IOptions<ApiVersioningOptions>>().Value;

        // Assert
        options.DefaultApiVersion.Should().Be(new ApiVersion(3));
        options.ReportApiVersions.Should().BeFalse();
    }

    [Fact]
    public void AddFoundationApiVersioning_ConfiguresApiExplorerOptions()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddFoundationApiVersioning(configuration);

        // Act — resolving IOptions triggers the AddApiExplorer lambda
        using ServiceProvider sp = services.BuildServiceProvider();
        ApiExplorerOptions options = sp.GetRequiredService<IOptions<ApiExplorerOptions>>().Value;

        // Assert
        options.GroupNameFormat.Should().Be("'v'VVV");
        options.SubstituteApiVersionInUrl.Should().BeTrue();
    }
}
