using System.Net;
using DigitalDynamics.Foundation.Diagnostics.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
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

    [Fact]
    public async Task StartupEndpoint_Returns503_WhenStartupCheckIsUnhealthy()
    {
        using HttpClient client = BuildTestClientWithStartupCheck(HealthStatus.Unhealthy);

        HttpResponseMessage response = await client.GetAsync("/health/startup", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task StartupEndpoint_Returns200_WhenStartupCheckIsDegraded()
    {
        using HttpClient client = BuildTestClientWithStartupCheck(HealthStatus.Degraded);

        HttpResponseMessage response = await client.GetAsync("/health/startup", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoints_Return200_WhenFallbackPolicyRequiresAuthentication()
    {
        // Regression guard: if a host adds SetFallbackPolicy(requireAuth), Kubernetes probes
        // must not receive 401 — AllowAnonymous on each endpoint bypasses the fallback policy.
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        builder.Services.AddHealthChecks();
        builder.Services.AddFoundationDiagnostics();

        WebApplication app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapFoundationHealthChecks();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();

        HttpResponseMessage live = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        HttpResponseMessage ready = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        HttpResponseMessage startup = await client.GetAsync("/health/startup", TestContext.Current.CancellationToken);

        live.StatusCode.Should().Be(HttpStatusCode.OK);
        ready.StatusCode.Should().Be(HttpStatusCode.OK);
        startup.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MapFoundationHealthChecks_WithConfigure_UsesCustomPaths()
    {
        // Arrange — override the liveness path via the configure delegate
        IHealthCheck fakeCheck = Substitute.For<IHealthCheck>();
        fakeCheck.CheckHealthAsync(Arg.Any<HealthCheckContext>(), Arg.Any<CancellationToken>())
            .Returns(HealthCheckResult.Healthy());

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks();
        builder.Services.AddFoundationDiagnostics();

        WebApplication app = builder.Build();
        app.MapFoundationHealthChecks(opts => opts.LivenessPath = "/alive");
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();

        // Act — the custom path must respond 200
        HttpResponseMessage response = await client.GetAsync("/alive", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

    private static HttpClient BuildTestClientWithStartupCheck(HealthStatus dependencyStatus)
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
            .Add(new HealthCheckRegistration("startup-dep", _ => fakeCheck, null, ["startup"]));

        builder.Services.AddFoundationDiagnostics();

        WebApplication app = builder.Build();
        app.MapFoundationHealthChecks();

        app.StartAsync().GetAwaiter().GetResult();

        return app.GetTestClient();
    }
}
