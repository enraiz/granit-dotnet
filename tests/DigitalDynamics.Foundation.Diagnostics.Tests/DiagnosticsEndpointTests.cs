using System.Net;
using DigitalDynamics.Foundation.Diagnostics.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Xunit;

namespace DigitalDynamics.Foundation.Diagnostics.Tests;

public sealed class DiagnosticsEndpointTests
{
    [Fact]
    public async Task LivenessEndpoint_Returns200_WhenDependencyIsUnhealthy()
    {
        // Liveness must never fail due to a dependency check
        using HttpClient client = BuildTestClient(dependencyStatus: HealthStatus.Unhealthy);

        HttpResponseMessage response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadinessEndpoint_Returns503_WhenDependencyIsUnhealthy()
    {
        using HttpClient client = BuildTestClient(dependencyStatus: HealthStatus.Unhealthy);

        HttpResponseMessage response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ReadinessEndpoint_Returns200_WhenDependencyIsDegraded()
    {
        // Degraded = pod stays in load balancer
        using HttpClient client = BuildTestClient(dependencyStatus: HealthStatus.Degraded);

        HttpResponseMessage response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadinessEndpoint_Returns200_WhenDependencyIsHealthy()
    {
        using HttpClient client = BuildTestClient(dependencyStatus: HealthStatus.Healthy);

        HttpResponseMessage response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LivenessEndpoint_ReturnsJsonContentType()
    {
        using HttpClient client = BuildTestClient(dependencyStatus: HealthStatus.Healthy);

        HttpResponseMessage response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    private static HttpClient BuildTestClient(HealthStatus dependencyStatus)
    {
        IHealthCheck fakeCheck = Substitute.For<IHealthCheck>();
        fakeCheck.CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(dependencyStatus switch
            {
                HealthStatus.Healthy => HealthCheckResult.Healthy(),
                HealthStatus.Degraded => HealthCheckResult.Degraded("degraded"),
                _ => HealthCheckResult.Unhealthy("unhealthy")
            });

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddHealthChecks()
            .Add(new HealthCheckRegistration("dep", _ => fakeCheck, null, ["readiness"]));

        builder.Services.AddFoundationDiagnostics();

        WebApplication app = builder.Build();
        app.MapFoundationHealthChecks();

        app.StartAsync().GetAwaiter().GetResult();

        return app.GetTestClient();
    }
}
