using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Granit.BackgroundJobs.Endpoints.Extensions;
using Granit.BackgroundJobs.Endpoints.Internal;
using Granit.Core.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Granit.BackgroundJobs.Endpoints.Tests;

/// <summary>
/// Integration tests for all background jobs administration endpoints.
/// Uses a TestServer + NSubstitute mock for IBackgroundJobManager.
/// A custom TestAuthHandler resolves authentication from X-Test-Roles header.
/// </summary>
public sealed class BackgroundJobsEndpointsTests : IAsyncDisposable
{
    private const string AdminRole = "granit-background-jobs-admin";
    private const string Prefix = "/background-jobs";

    private readonly IBackgroundJobManager _manager = Substitute.For<IBackgroundJobManager>();
    private readonly WebApplication _app;

    // Admin client: authenticated with the admin role.
    private readonly HttpClient _adminClient;

    // Authenticated client without the admin role.
    private readonly HttpClient _userClient;

    // Unauthenticated client.
    private readonly HttpClient _anonClient;

    public BackgroundJobsEndpointsTests()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });

        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(_manager);

        _app = builder.Build();
        _app.MapBackgroundJobsEndpoints();
        _app.StartAsync().GetAwaiter().GetResult();

        _adminClient = BuildClient(AdminRole);
        _userClient = BuildClient("regular-user");
        _anonClient = _app.GetTestClient(); // no auth header
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    // ── GET / ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithAdminToken_Returns200WithList()
    {
        // Arrange
        IReadOnlyList<BackgroundJobStatus> jobs =
        [
            BuildStatus("daily-report", isEnabled: true),
            BuildStatus("monthly-export", isEnabled: true),
            BuildStatus("weekly-cleanup", isEnabled: false),
        ];
        _manager.GetAllAsync(Arg.Any<CancellationToken>()).Returns(jobs);

        // Act
        HttpResponseMessage response = await _adminClient.GetAsync(Prefix, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        IReadOnlyList<BackgroundJobStatus>? result =
            await response.Content.ReadFromJsonAsync<IReadOnlyList<BackgroundJobStatus>>(
                TestContext.Current.CancellationToken);
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        HttpResponseMessage response = await _anonClient.GetAsync(Prefix, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithWrongRole_Returns403()
    {
        HttpResponseMessage response = await _userClient.GetAsync(Prefix, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /{name} ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByName_WhenJobExists_Returns200()
    {
        // Arrange
        BackgroundJobStatus job = BuildStatus("daily-report", isEnabled: true);
        _manager.FindAsync("daily-report", Arg.Any<CancellationToken>()).Returns(job);

        // Act
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/daily-report", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        BackgroundJobStatus? result =
            await response.Content.ReadFromJsonAsync<BackgroundJobStatus>(
                TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.JobName.Should().Be("daily-report");
    }

    [Fact]
    public async Task GetByName_WhenJobNotFound_Returns404()
    {
        // Arrange
        _manager.FindAsync("ghost-job", Arg.Any<CancellationToken>()).Returns((BackgroundJobStatus?)null);

        // Act
        HttpResponseMessage response = await _adminClient.GetAsync(
            $"{Prefix}/ghost-job", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /{name}/pause ────────────────────────────────────────────────────

    [Fact]
    public async Task Pause_WhenJobExists_Returns204()
    {
        // Arrange
        _manager.PauseAsync("daily-report", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(
            $"{Prefix}/daily-report/pause", content: null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _manager.Received(1).PauseAsync("daily-report", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pause_WhenJobNotFound_Returns404()
    {
        // Arrange
        _manager.PauseAsync("ghost-job", Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntityNotFoundException(typeof(BackgroundJobDefinition), "ghost-job"));

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(
            $"{Prefix}/ghost-job/pause", content: null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /{name}/resume ───────────────────────────────────────────────────

    [Fact]
    public async Task Resume_WhenJobExists_Returns204()
    {
        // Arrange
        _manager.ResumeAsync("daily-report", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(
            $"{Prefix}/daily-report/resume", content: null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await _manager.Received(1).ResumeAsync("daily-report", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resume_WhenJobNotFound_Returns404()
    {
        // Arrange
        _manager.ResumeAsync("ghost-job", Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntityNotFoundException(typeof(BackgroundJobDefinition), "ghost-job"));

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(
            $"{Prefix}/ghost-job/resume", content: null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /{name}/trigger ──────────────────────────────────────────────────

    [Fact]
    public async Task Trigger_WhenJobExists_Returns202()
    {
        // Arrange — TriggerNowAsync is fire-and-forget (async enqueue)
        _manager.TriggerNowAsync("monthly-export", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(
            $"{Prefix}/monthly-export/trigger", content: null, TestContext.Current.CancellationToken);

        // Assert — 202 Accepted, not 200 (processing is async via Wolverine)
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await _manager.Received(1).TriggerNowAsync("monthly-export", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Trigger_WhenJobNotFound_Returns404()
    {
        // Arrange
        _manager.TriggerNowAsync("ghost-job", Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntityNotFoundException(typeof(BackgroundJobDefinition), "ghost-job"));

        // Act
        HttpResponseMessage response = await _adminClient.PostAsync(
            $"{Prefix}/ghost-job/trigger", content: null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Security: custom role via options ─────────────────────────────────────

    [Fact]
    public async Task MapBackgroundJobsEndpoints_WithCustomRole_EnforcesCustomRole()
    {
        // Arrange — separate app with a custom required role
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(_manager);
        _manager.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        await using WebApplication customApp = builder.Build();
        customApp.MapBackgroundJobsEndpoints(opts => opts.RequiredRole = "ops-team");
        await customApp.StartAsync(TestContext.Current.CancellationToken);

        // Client with "ops-team" role
        using HttpClient opsClient = customApp.GetTestClient();
        opsClient.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, "ops-team");

        // Client with admin role (should be rejected since policy expects ops-team)
        using HttpClient adminClient = customApp.GetTestClient();
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, AdminRole);

        // Act
        HttpResponseMessage opsResponse = await opsClient.GetAsync(
            "/background-jobs", TestContext.Current.CancellationToken);
        HttpResponseMessage adminResponse = await adminClient.GetAsync(
            "/background-jobs", TestContext.Current.CancellationToken);

        // Assert
        opsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        adminResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private HttpClient BuildClient(string role)
    {
        HttpClient client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, role);
        return client;
    }

    private static BackgroundJobStatus BuildStatus(string name, bool isEnabled) =>
        new(
            JobName: name,
            CronExpression: "0 8 * * *",
            IsEnabled: isEnabled,
            LastExecutedAt: null,
            NextExecutionAt: null,
            ConsecutiveFailures: 0,
            DeadLetterCount: 0,
            LastError: null);

    // ── Fake authentication handler ───────────────────────────────────────────

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";
        public const string RolesHeader = "X-Test-Roles";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(RolesHeader, out Microsoft.Extensions.Primitives.StringValues rolesHeader))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            string[] roles = rolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            Claim[] claims =
            [
                new(ClaimTypes.Name, "test-user"),
                .. roles.Select(r => new Claim(ClaimTypes.Role, r.Trim())),
            ];

            ClaimsIdentity identity = new(claims, SchemeName);
            ClaimsPrincipal principal = new(identity);
            AuthenticationTicket ticket = new(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
