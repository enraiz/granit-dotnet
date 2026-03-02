using Granit.Authorization.Abstractions;
using Granit.Core.MultiTenancy;
using Granit.Workflow.Notifications.Keycloak;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Workflow.Notifications.Tests;

public sealed class KeycloakApproverResolverTests : IDisposable
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly IPermissionManager _permissionManager = Substitute.For<IPermissionManager>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private readonly KeycloakAdminOptions _options = new()
    {
        BaseUrl = "https://keycloak.test",
        Realm = "test-realm",
        ClientId = "admin-service",
        ClientSecret = "secret",
    };

    private readonly KeycloakAdminTokenService _tokenService;
    private readonly KeycloakApproverResolver _resolver;

    public KeycloakApproverResolverTests()
    {
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://keycloak.test/") };
        _httpClientFactory.CreateClient("KeycloakAdmin").Returns(_httpClient);
        _currentTenant.IsAvailable.Returns(false);

        // Token service returns a fake token.
        MockHttpMessageHandler tokenHandler = new()
        {
            ResponseBody = """{"access_token":"fake-token","expires_in":300}""",
        };
        HttpClient tokenClient = new(tokenHandler) { BaseAddress = new Uri("https://keycloak.test/") };
        IHttpClientFactory tokenFactory = Substitute.For<IHttpClientFactory>();
        tokenFactory.CreateClient("KeycloakAdmin").Returns(tokenClient);

        _tokenService = new KeycloakAdminTokenService(
            tokenFactory,
            Options.Create(_options),
            NullLogger<KeycloakAdminTokenService>.Instance);

        _resolver = new KeycloakApproverResolver(
            _permissionManager,
            _tokenService,
            _httpClientFactory,
            Options.Create(_options),
            _currentTenant,
            NullLogger<KeycloakApproverResolver>.Instance);
    }

    [Fact]
    public async Task ResolveApproversAsync_WithRolesAndUsers_ReturnsUserIds()
    {
        _permissionManager.GetGrantedRolesAsync("workflow.publish", null, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "editor" });

        _handler.ResponseBody = """[{"id":"user-1"},{"id":"user-2"}]""";

        IReadOnlyList<string> result = await _resolver.ResolveApproversAsync(
            "workflow.publish", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result.ShouldContain("user-1");
        result.ShouldContain("user-2");
    }

    [Fact]
    public async Task ResolveApproversAsync_NoRolesGranted_ReturnsEmptyList()
    {
        _permissionManager.GetGrantedRolesAsync("workflow.publish", null, Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        IReadOnlyList<string> result = await _resolver.ResolveApproversAsync(
            "workflow.publish", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveApproversAsync_RoleWithNoUsers_ReturnsEmptyList()
    {
        _permissionManager.GetGrantedRolesAsync("workflow.publish", null, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "editor" });

        _handler.ResponseBody = "[]";

        IReadOnlyList<string> result = await _resolver.ResolveApproversAsync(
            "workflow.publish", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveApproversAsync_KeycloakError_ReturnsEmptyList()
    {
        _permissionManager.GetGrantedRolesAsync("workflow.publish", null, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "editor" });

        _handler.ResponseStatusCode = System.Net.HttpStatusCode.ServiceUnavailable;
        _handler.ResponseBody = string.Empty;

        IReadOnlyList<string> result = await _resolver.ResolveApproversAsync(
            "workflow.publish", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveApproversAsync_DuplicateUsersAcrossRoles_ReturnsDeduplicatedList()
    {
        _permissionManager.GetGrantedRolesAsync("workflow.publish", null, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "editor", "admin" });

        // Override handler to return different responses per call.
        MockSequenceHttpMessageHandler sequenceHandler = new(
        [
            """[{"id":"shared-user"},{"id":"editor-user"}]""",
            """[{"id":"shared-user"},{"id":"admin-user"}]""",
        ]);

        HttpClient sequenceClient = new(sequenceHandler) { BaseAddress = new Uri("https://keycloak.test/") };
        IHttpClientFactory sequenceFactory = Substitute.For<IHttpClientFactory>();
        sequenceFactory.CreateClient("KeycloakAdmin").Returns(sequenceClient);

        KeycloakApproverResolver resolver = new(
            _permissionManager,
            _tokenService,
            sequenceFactory,
            Options.Create(_options),
            _currentTenant,
            NullLogger<KeycloakApproverResolver>.Instance);

        IReadOnlyList<string> result = await resolver.ResolveApproversAsync(
            "workflow.publish", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(3);
        result.ShouldContain("shared-user");
        result.ShouldContain("editor-user");
        result.ShouldContain("admin-user");
    }

    [Fact]
    public async Task ResolveApproversAsync_WithTenant_PassesTenantId()
    {
        Guid tenantId = Guid.NewGuid();
        _currentTenant.IsAvailable.Returns(true);
        _currentTenant.Id.Returns(tenantId);

        _permissionManager.GetGrantedRolesAsync("workflow.publish", tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "editor" });

        _handler.ResponseBody = """[{"id":"user-1"}]""";

        IReadOnlyList<string> result = await _resolver.ResolveApproversAsync(
            "workflow.publish", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        await _permissionManager.Received(1).GetGrantedRolesAsync(
            "workflow.publish", tenantId, Arg.Any<CancellationToken>());
    }

    public void Dispose() => _httpClient.Dispose();
}

/// <summary>
/// Handler that returns different responses for sequential requests.
/// </summary>
internal sealed class MockSequenceHttpMessageHandler(IReadOnlyList<string> responses) : HttpMessageHandler
{
    private int _callIndex;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int index = Math.Min(_callIndex++, responses.Count - 1);
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(responses[index], System.Text.Encoding.UTF8, "application/json"),
        });
    }
}
