using Granit.BackgroundJobs.Endpoints.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace Granit.BackgroundJobs.Endpoints.Tests;

public sealed class MapBackgroundJobsEndpointsTests
{
    [Fact]
    public void MapBackgroundJobsEndpoints_WithDefaultOptions_ReturnsRouteGroupBuilder()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        WebApplication app = builder.Build();

        // Act
        RouteGroupBuilder group = app.MapBackgroundJobsEndpoints();

        // Assert
        group.ShouldNotBeNull();
    }

    [Fact]
    public void MapBackgroundJobsEndpoints_WithCustomPrefix_ReturnsRouteGroupBuilder()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        WebApplication app = builder.Build();

        // Act
        RouteGroupBuilder group = app.MapBackgroundJobsEndpoints(
            opts => opts.RoutePrefix = "admin/jobs");

        // Assert
        group.ShouldNotBeNull();
    }

}
