using Granit.Notifications.Endpoints.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Endpoints.Tests;

public sealed class MapGranitNotificationEndpointsTests
{
    [Fact]
    public void MapGranitNotificationEndpoints_WithDefaultOptions_ReturnsEndpointRouteBuilder()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        WebApplication app = builder.Build();

        IEndpointRouteBuilder result = app.MapGranitNotificationEndpoints();

        result.ShouldNotBeNull();
    }

    [Fact]
    public void MapGranitNotificationEndpoints_WithCustomPrefix_ReturnsEndpointRouteBuilder()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        WebApplication app = builder.Build();

        IEndpointRouteBuilder result = app.MapGranitNotificationEndpoints(
            opts => opts.RoutePrefix = "notif");

        result.ShouldNotBeNull();
    }

    [Fact]
    public void MapGranitNotificationEndpoints_WithApiPrefix_ReturnsEndpointRouteBuilder()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        WebApplication app = builder.Build();

        IEndpointRouteBuilder result = app.MapGranitNotificationEndpoints(
            opts => opts.ApiPrefix = "api/v1");

        result.ShouldNotBeNull();
    }

    [Fact]
    public void MapGranitNotificationEndpoints_WithNullConfigure_DoesNotThrow()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        WebApplication app = builder.Build();

        Should.NotThrow(() => app.MapGranitNotificationEndpoints(configure: null));
    }
}
