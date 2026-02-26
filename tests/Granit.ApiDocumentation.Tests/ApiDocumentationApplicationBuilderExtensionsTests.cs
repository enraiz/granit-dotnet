// =============================================================================
// Tests - ApiDocumentationApplicationBuilderExtensions
// =============================================================================
// Vérifie que UseGranitApiDocumentation active les endpoints OpenAPI/Scalar
// en Development et en Production si EnableInProduction est true, et est no-op
// sinon.
// =============================================================================

using FluentAssertions;
using Granit.ApiDocumentation.Extensions;
using Granit.ApiDocumentation.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Granit.ApiDocumentation.Tests;

public sealed class ApiDocumentationApplicationBuilderExtensionsTests
{
    // --- Production + EnableInProduction=false → returns app without mapping routes ---

    [Fact]
    public void UseGranitApiDocumentation_ProductionDisabled_ReturnsAppWithoutMappingRoutes()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Production;
        builder.Services.AddGranitApiDocumentation(opts =>
        {
            opts.MajorVersions = [1];
            opts.EnableInProduction = false;
        });
        WebApplication app = builder.Build();

        // Act
        WebApplication result = app.UseGranitApiDocumentation();

        // Assert
        result.Should().BeSameAs(app);
    }

    // --- Development → maps routes, returns app ---

    [Fact]
    public void UseGranitApiDocumentation_Development_MapsRoutesAndReturnsApp()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddGranitApiDocumentation(opts =>
        {
            opts.Title = "Test API";
            opts.MajorVersions = [1];
        });
        WebApplication app = builder.Build();

        // Act
        WebApplication result = app.UseGranitApiDocumentation();

        // Assert
        result.Should().BeSameAs(app);
    }

    // --- Production + EnableInProduction=true → maps routes, returns app ---

    [Fact]
    public void UseGranitApiDocumentation_ProductionEnabled_MapsRoutesAndReturnsApp()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Production;
        builder.Services.AddGranitApiDocumentation(opts =>
        {
            opts.Title = "Prod API";
            opts.MajorVersions = [1, 2];
            opts.EnableInProduction = true;
        });
        WebApplication app = builder.Build();

        // Act
        WebApplication result = app.UseGranitApiDocumentation();

        // Assert
        result.Should().BeSameAs(app);
    }

    // --- AuthorizationPolicy = null → no explicit policy applied ---

    [Fact]
    public void UseGranitApiDocumentation_NullPolicy_MapsRoutesWithoutPolicy()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddGranitApiDocumentation(opts =>
        {
            opts.MajorVersions = [1];
            opts.AuthorizationPolicy = null;
        });
        WebApplication app = builder.Build();

        // Act
        WebApplication result = app.UseGranitApiDocumentation();

        // Assert
        result.Should().BeSameAs(app);
    }

    // --- AuthorizationPolicy = "" → AllowAnonymous applied ---

    [Fact]
    public void UseGranitApiDocumentation_EmptyPolicy_MapsRoutesWithAllowAnonymous()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddGranitApiDocumentation(opts =>
        {
            opts.MajorVersions = [1];
            opts.AuthorizationPolicy = "";
        });
        WebApplication app = builder.Build();

        // Act
        WebApplication result = app.UseGranitApiDocumentation();

        // Assert
        result.Should().BeSameAs(app);
    }

    // --- AuthorizationPolicy = "InternalDeveloper" → RequireAuthorization applied ---

    [Fact]
    public void UseGranitApiDocumentation_NamedPolicy_MapsRoutesWithAuthorization()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("InternalDeveloper", p => p.RequireAuthenticatedUser());
        builder.Services.AddGranitApiDocumentation(opts =>
        {
            opts.MajorVersions = [1];
            opts.AuthorizationPolicy = "InternalDeveloper";
        });
        WebApplication app = builder.Build();

        // Act
        WebApplication result = app.UseGranitApiDocumentation();

        // Assert
        result.Should().BeSameAs(app);
    }
}
