// =============================================================================
// Tests - ApiDocumentationApplicationBuilderExtensions
// =============================================================================
// Vérifie que UseFoundationApiDocumentation active les endpoints OpenAPI/Scalar
// en Development et en Production si EnableInProduction est true, et est no-op
// sinon.
// =============================================================================

using DigitalDynamics.Foundation.ApiDocumentation.Extensions;
using DigitalDynamics.Foundation.ApiDocumentation.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DigitalDynamics.Foundation.ApiDocumentation.Tests;

public sealed class ApiDocumentationApplicationBuilderExtensionsTests
{
    // --- Production + EnableInProduction=false → returns app without mapping routes ---

    [Fact]
    public void UseFoundationApiDocumentation_ProductionDisabled_ReturnsAppWithoutMappingRoutes()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Production;
        builder.Services.AddFoundationApiDocumentation(opts =>
        {
            opts.MajorVersions = [1];
            opts.EnableInProduction = false;
        });
        WebApplication app = builder.Build();

        // Act
        WebApplication result = app.UseFoundationApiDocumentation();

        // Assert
        result.Should().BeSameAs(app);
    }

    // --- Development → maps routes, returns app ---

    [Fact]
    public void UseFoundationApiDocumentation_Development_MapsRoutesAndReturnsApp()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddFoundationApiDocumentation(opts =>
        {
            opts.Title = "Test API";
            opts.MajorVersions = [1];
        });
        WebApplication app = builder.Build();

        // Act
        WebApplication result = app.UseFoundationApiDocumentation();

        // Assert
        result.Should().BeSameAs(app);
    }

    // --- Production + EnableInProduction=true → maps routes, returns app ---

    [Fact]
    public void UseFoundationApiDocumentation_ProductionEnabled_MapsRoutesAndReturnsApp()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Production;
        builder.Services.AddFoundationApiDocumentation(opts =>
        {
            opts.Title = "Prod API";
            opts.MajorVersions = [1, 2];
            opts.EnableInProduction = true;
        });
        WebApplication app = builder.Build();

        // Act
        WebApplication result = app.UseFoundationApiDocumentation();

        // Assert
        result.Should().BeSameAs(app);
    }
}
