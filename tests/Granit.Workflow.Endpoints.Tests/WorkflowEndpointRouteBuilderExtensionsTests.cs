using System.Net;
using System.Net.Http.Json;
using Granit.Workflow.Dtos;
using Granit.Workflow.Endpoints.Extensions;
using Granit.Workflow.Endpoints.Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Endpoints.Tests;

/// <summary>
/// Verifies that <see cref="WorkflowEndpointRouteBuilderExtensions.MapWorkflowEndpoints"/>
/// correctly applies route prefix, API prefix, and authorization policy.
/// </summary>
public sealed class WorkflowEndpointRouteBuilderExtensionsTests
{
    [Fact]
    public void PolicyName_equals_History_Default() =>
        WorkflowAuthorizationPolicy.PolicyName.ShouldBe("Workflow.History");

    [Fact]
    public async Task MapWorkflowEndpoints_with_custom_prefix_routes_correctly()
    {
        // Arrange
        IWorkflowHistoryQuery historyQuery = Substitute.For<IWorkflowHistoryQuery>();
        historyQuery.GetHistoryAsync("Order", "1", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TransitionHistoryResponse>());

        await using WebApplication app = BuildApp(historyQuery, opts =>
        {
            opts.RoutePrefix = "admin/wf";
        });

        HttpClient client = BuildAdminClient(app);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/admin/wf/Order/1/history", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MapWorkflowEndpoints_with_api_prefix_prepends_prefix()
    {
        // Arrange
        IWorkflowHistoryQuery historyQuery = Substitute.For<IWorkflowHistoryQuery>();
        historyQuery.GetHistoryAsync("Order", "1", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TransitionHistoryResponse>());

        await using WebApplication app = BuildApp(historyQuery, opts =>
        {
            opts.ApiPrefix = "api/v1";
            opts.RoutePrefix = "workflow";
        });

        HttpClient client = BuildAdminClient(app);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/api/v1/workflow/Order/1/history", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MapWorkflowEndpoints_with_api_prefix_trims_slashes()
    {
        // Arrange: trailing slash on ApiPrefix, leading slash on RoutePrefix
        IWorkflowHistoryQuery historyQuery = Substitute.For<IWorkflowHistoryQuery>();
        historyQuery.GetHistoryAsync("Order", "1", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TransitionHistoryResponse>());

        await using WebApplication app = BuildApp(historyQuery, opts =>
        {
            opts.ApiPrefix = "api/v2/";
            opts.RoutePrefix = "/workflow";
        });

        HttpClient client = BuildAdminClient(app);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/api/v2/workflow/Order/1/history", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MapWorkflowEndpoints_with_custom_role_accepts_custom_role()
    {
        // Arrange
        IWorkflowHistoryQuery historyQuery = Substitute.For<IWorkflowHistoryQuery>();
        historyQuery.GetHistoryAsync("Order", "1", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TransitionHistoryResponse>());

        await using WebApplication app = BuildApp(historyQuery, opts =>
        {
            opts.RequiredRole = "ops-team";
        });

        HttpClient client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, "ops-team");

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/workflow/Order/1/history", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MapWorkflowEndpoints_with_custom_role_rejects_default_role()
    {
        // Arrange
        IWorkflowHistoryQuery historyQuery = Substitute.For<IWorkflowHistoryQuery>();

        await using WebApplication app = BuildApp(historyQuery, opts =>
        {
            opts.RequiredRole = "ops-team";
        });

        HttpClient client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, "granit-workflow-admin");

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/workflow/Order/1/history", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MapWorkflowEndpoints_default_prefix_is_workflow()
    {
        // Arrange
        IWorkflowHistoryQuery historyQuery = Substitute.For<IWorkflowHistoryQuery>();
        historyQuery.GetHistoryAsync("Order", "1", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TransitionHistoryResponse>());

        await using WebApplication app = BuildApp(historyQuery);
        HttpClient client = BuildAdminClient(app);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/workflow/Order/1/history", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MapWorkflowEndpoints_returns_group_builder_for_chaining()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Substitute.For<IWorkflowHistoryQuery>());

        WebApplication app = builder.Build();

        // Act
        Microsoft.AspNetCore.Routing.RouteGroupBuilder group = app.MapWorkflowEndpoints();

        // Assert
        group.ShouldNotBeNull();

        await app.DisposeAsync();
    }

    // -- Helpers ---------------------------------------------------------------

    private static WebApplication BuildApp(
        IWorkflowHistoryQuery historyQuery,
        Action<WorkflowEndpointsOptions>? configure = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });

        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(historyQuery);

        WebApplication app = builder.Build();
        app.MapWorkflowEndpoints(configure);
        app.StartAsync().GetAwaiter().GetResult();

        return app;
    }

    private static HttpClient BuildAdminClient(WebApplication app)
    {
        HttpClient client = app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, "granit-workflow-admin");
        return client;
    }
}
