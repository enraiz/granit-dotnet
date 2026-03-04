using Granit.Authorization.Endpoints.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace Granit.Authorization.Endpoints.Tests;

public sealed class MapAuthorizationEndpointsTests
{
    [Fact]
    public void MapAuthorizationEndpoints_WithDefaultOptions_ReturnsRouteGroupBuilder()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        WebApplication app = builder.Build();

        // Act
        RouteGroupBuilder group = app.MapAuthorizationEndpoints();

        // Assert
        group.ShouldNotBeNull();
    }

    [Fact]
    public void MapAuthorizationEndpoints_WithCustomPrefix_ReturnsRouteGroupBuilder()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        WebApplication app = builder.Build();

        // Act
        RouteGroupBuilder group = app.MapAuthorizationEndpoints(
            opts => opts.RoutePrefix = "authorization");

        // Assert
        group.ShouldNotBeNull();
    }

    [Fact]
    public void MapAuthorizationEndpoints_WithApiPrefix_ReturnsRouteGroupBuilder()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        WebApplication app = builder.Build();

        // Act
        RouteGroupBuilder group = app.MapAuthorizationEndpoints(
            opts => opts.ApiPrefix = "api/v1");

        // Assert
        group.ShouldNotBeNull();
    }
}
