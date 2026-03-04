using System.Net;
using System.Net.Http.Json;
using Granit.Workflow.Dtos;
using Granit.Workflow.Endpoints.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Endpoints.Tests;

/// <summary>
/// Integration tests for the workflow read endpoints (GET /history).
/// Uses a real <see cref="WebApplication"/> with <see cref="TestServer"/>.
/// </summary>
public sealed class WorkflowReadEndpointsTests : IAsyncDisposable
{
    private const string AdminRole = "granit-workflow-admin";
    private const string WorkflowPrefix = "/workflow";

    private readonly IWorkflowHistoryQuery _historyQuery = Substitute.For<IWorkflowHistoryQuery>();
    private readonly WebApplication _app;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _anonClient;

    public WorkflowReadEndpointsTests()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });

        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(_historyQuery);

        _app = builder.Build();
        _app.MapWorkflowEndpoints();
        _app.StartAsync().GetAwaiter().GetResult();

        _adminClient = BuildClient(AdminRole);
        _anonClient = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    // -- GET /{entityType}/{entityId}/history ---------------------------------

    [Fact]
    public async Task GetHistory_returns_history_entries()
    {
        // Arrange
        _historyQuery.GetHistoryAsync("Order", "42", Arg.Any<CancellationToken>())
            .Returns(new List<TransitionHistoryResponse>
            {
                new("Draft", "Submitted", DateTimeOffset.UtcNow.AddHours(-2), "user-1", null),
                new("Submitted", "Approved", DateTimeOffset.UtcNow.AddHours(-1), "user-2", "LGTM"),
            });

        // Act
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{WorkflowPrefix}/Order/42/history", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TransitionHistoryResponse[]? history = await response.Content
            .ReadFromJsonAsync<TransitionHistoryResponse[]>(TestContext.Current.CancellationToken);
        history.ShouldNotBeNull();
        history!.Length.ShouldBe(2);
        history[0].PreviousState.ShouldBe("Draft");
        history[0].NewState.ShouldBe("Submitted");
        history[1].NewState.ShouldBe("Approved");
        history[1].Comment.ShouldBe("LGTM");
    }

    [Fact]
    public async Task GetHistory_empty_returns_empty_list()
    {
        // Arrange
        _historyQuery.GetHistoryAsync("Order", "99", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TransitionHistoryResponse>());

        // Act
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{WorkflowPrefix}/Order/99/history", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TransitionHistoryResponse[]? history = await response.Content
            .ReadFromJsonAsync<TransitionHistoryResponse[]>(TestContext.Current.CancellationToken);
        history.ShouldNotBeNull();
        history!.Length.ShouldBe(0);
    }

    [Fact]
    public async Task GetHistory_without_auth_returns_401()
    {
        // Act
        HttpResponseMessage response = await _anonClient.GetAsync(
            $"{WorkflowPrefix}/Order/42/history", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHistory_with_wrong_role_returns_403()
    {
        // Arrange
        HttpClient wrongRoleClient = BuildClient("some-other-role");

        // Act
        HttpResponseMessage response = await wrongRoleClient.GetAsync(
            $"{WorkflowPrefix}/Order/42/history", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetHistory_preserves_comment_null()
    {
        // Arrange
        _historyQuery.GetHistoryAsync("Invoice", "7", Arg.Any<CancellationToken>())
            .Returns(new List<TransitionHistoryResponse>
            {
                new("Draft", "Sent", DateTimeOffset.UtcNow, "user-1", null),
            });

        // Act
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{WorkflowPrefix}/Invoice/7/history", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TransitionHistoryResponse[]? history = await response.Content
            .ReadFromJsonAsync<TransitionHistoryResponse[]>(TestContext.Current.CancellationToken);
        history.ShouldNotBeNull();
        history![0].Comment.ShouldBeNull();
    }

    // -- Helpers ---------------------------------------------------------------

    private HttpClient BuildClient(string role)
    {
        HttpClient client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, role);
        return client;
    }
}
