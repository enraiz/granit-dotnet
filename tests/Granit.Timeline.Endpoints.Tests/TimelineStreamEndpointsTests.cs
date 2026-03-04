using System.Net;
using System.Net.Http.Json;
using Granit.Timeline.Abstractions;
using Granit.Timeline.Endpoints.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Timeline.Endpoints.Tests;

/// <summary>
/// Integration tests for the GET stream endpoint.
/// </summary>
public sealed class TimelineStreamEndpointsTests : IAsyncDisposable
{
    private const string UserRole = "granit-timeline-user";
    private const string Prefix = "/timeline";

    private readonly ITimelineQuery _query = Substitute.For<ITimelineQuery>();
    private readonly WebApplication _app;
    private readonly HttpClient _authClient;
    private readonly HttpClient _anonClient;

    public TimelineStreamEndpointsTests()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });

        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(_query);

        // Required by follower/entry endpoints but not exercised here
        builder.Services.AddSingleton(Substitute.For<ITimelineStore>());
        builder.Services.AddSingleton(Substitute.For<ITimelineFollowerService>());
        builder.Services.AddSingleton(Substitute.For<ITimelineNotifier>());
        builder.Services.AddSingleton(Substitute.For<Granit.Security.ICurrentUserService>());

        _app = builder.Build();
        _app.MapTimelineEndpoints();
        _app.StartAsync().GetAwaiter().GetResult();

        _authClient = BuildClient(UserRole);
        _anonClient = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    // -- GET /{entityType}/{entityId} -----------------------------------------

    [Fact]
    public async Task GetStream_returns_paginated_entries()
    {
        // Arrange
        var entry = new TimelineStreamEntry
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            EntryType = TimelineStreamEntryType.Comment,
            AuthorId = "user-1",
            AuthorName = "Alice",
            Body = "Hello",
        };

        _query.GetStreamAsync("Patient", "42", 0, 20, Arg.Any<CancellationToken>())
            .Returns(new TimelineStreamPage { Items = [entry], TotalCount = 1 });

        // Act
        HttpResponseMessage response = await _authClient.GetAsync(
            $"{Prefix}/Patient/42", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TimelineStreamPage? page = await response.Content
            .ReadFromJsonAsync<TimelineStreamPage>(TestContext.Current.CancellationToken);
        page.ShouldNotBeNull();
        page!.TotalCount.ShouldBe(1);
        page.Items.Count.ShouldBe(1);
        page.Items[0].Body.ShouldBe("Hello");
        page.Items[0].AuthorName.ShouldBe("Alice");
    }

    [Fact]
    public async Task GetStream_empty_returns_empty_page()
    {
        // Arrange
        _query.GetStreamAsync("Invoice", "99", 0, 20, Arg.Any<CancellationToken>())
            .Returns(new TimelineStreamPage { Items = [], TotalCount = 0 });

        // Act
        HttpResponseMessage response = await _authClient.GetAsync(
            $"{Prefix}/Invoice/99", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TimelineStreamPage? page = await response.Content
            .ReadFromJsonAsync<TimelineStreamPage>(TestContext.Current.CancellationToken);
        page.ShouldNotBeNull();
        page!.TotalCount.ShouldBe(0);
        page.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetStream_passes_skip_and_take_parameters()
    {
        // Arrange
        _query.GetStreamAsync("Patient", "1", 5, 10, Arg.Any<CancellationToken>())
            .Returns(new TimelineStreamPage { Items = [], TotalCount = 50 });

        // Act
        HttpResponseMessage response = await _authClient.GetAsync(
            $"{Prefix}/Patient/1?skip=5&take=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await _query.Received(1).GetStreamAsync("Patient", "1", 5, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStream_without_auth_returns_401()
    {
        // Act
        HttpResponseMessage response = await _anonClient.GetAsync(
            $"{Prefix}/Patient/42", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStream_with_wrong_role_returns_403()
    {
        // Arrange
        HttpClient wrongRoleClient = BuildClient("some-other-role");

        // Act
        HttpResponseMessage response = await wrongRoleClient.GetAsync(
            $"{Prefix}/Patient/42", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // -- Helpers ---------------------------------------------------------------

    private HttpClient BuildClient(string role)
    {
        HttpClient client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, role);
        return client;
    }
}
