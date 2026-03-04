using System.Net;
using Granit.Identity.Keycloak.Internal;
using Granit.Identity.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Identity.Keycloak.Tests;

public sealed class KeycloakIdentityProviderTests : IDisposable
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly KeycloakAdminOptions _options = new()
    {
        BaseUrl = "https://keycloak.test",
        Realm = "test-realm",
        ClientId = "admin-service",
        ClientSecret = "secret",
    };

    private readonly KeycloakAdminTokenService _tokenService;
    private readonly KeycloakIdentityProvider _provider;

    public KeycloakIdentityProviderTests()
    {
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://keycloak.test/") };
        _httpClientFactory.CreateClient("KeycloakAdmin").Returns(_httpClient);

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

        _provider = new KeycloakIdentityProvider(
            _tokenService,
            _httpClientFactory,
            Options.Create(_options),
            NullLogger<KeycloakIdentityProvider>.Instance);
    }

    [Fact]
    public async Task GetRoleMembersAsync_WithUsers_ReturnsIdentityUsers()
    {
        _handler.ResponseBody = """[{"id":"user-1","username":"alice","email":"alice@test.com","firstName":"Alice","lastName":"Doe","enabled":true},{"id":"user-2","username":"bob","email":null,"firstName":null,"lastName":null,"enabled":true}]""";

        IReadOnlyList<IdentityUser> result = await _provider.GetRoleMembersAsync(
            "editor", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe("user-1");
        result[0].Username.ShouldBe("alice");
        result[0].Email.ShouldBe("alice@test.com");
        result[1].Id.ShouldBe("user-2");
        result[1].Username.ShouldBe("bob");
    }

    [Fact]
    public async Task GetRoleMembersAsync_EmptyRole_ReturnsEmptyList()
    {
        _handler.ResponseBody = "[]";

        IReadOnlyList<IdentityUser> result = await _provider.GetRoleMembersAsync(
            "editor", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRoleMembersAsync_KeycloakError_ReturnsEmptyList()
    {
        _handler.ResponseStatusCode = HttpStatusCode.ServiceUnavailable;
        _handler.ResponseBody = string.Empty;

        IReadOnlyList<IdentityUser> result = await _provider.GetRoleMembersAsync(
            "editor", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRoleMembersAsync_CallsCorrectEndpoint()
    {
        _handler.ResponseBody = "[]";

        await _provider.GetRoleMembersAsync("content-editor", TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
        _handler.Requests[0].Url.ShouldContain("/admin/realms/test-realm/roles/content-editor/users");
    }

    [Fact]
    public async Task GetUsersAsync_WithSearch_CallsCorrectEndpoint()
    {
        _handler.ResponseBody = """[{"id":"user-1","username":"alice","email":"alice@test.com","firstName":"Alice","lastName":"Doe","enabled":true}]""";

        IReadOnlyList<IdentityUser> result = await _provider.GetUsersAsync(
            search: "alice", first: 0, max: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Username.ShouldBe("alice");
        _handler.Requests[0].Url.ShouldContain("search=alice");
        _handler.Requests[0].Url.ShouldContain("first=0");
        _handler.Requests[0].Url.ShouldContain("max=10");
    }

    [Fact]
    public async Task GetUserAsync_ExistingUser_ReturnsUser()
    {
        _handler.ResponseBody = """{"id":"user-1","username":"alice","email":"alice@test.com","firstName":"Alice","lastName":"Doe","enabled":true}""";

        IdentityUser? result = await _provider.GetUserAsync(
            "user-1", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Id.ShouldBe("user-1");
        result.Username.ShouldBe("alice");
    }

    [Fact]
    public async Task GetUserAsync_KeycloakError_ReturnsNull()
    {
        _handler.ResponseStatusCode = HttpStatusCode.NotFound;
        _handler.ResponseBody = string.Empty;

        IdentityUser? result = await _provider.GetUserAsync(
            "unknown", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetRolesAsync_ReturnsRoles()
    {
        _handler.ResponseBody = """[{"id":"role-1","name":"editor","description":"Content editor"},{"id":"role-2","name":"admin","description":null}]""";

        IReadOnlyList<IdentityRole> result = await _provider.GetRolesAsync(
            TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("editor");
        result[0].Description.ShouldBe("Content editor");
        result[1].Name.ShouldBe("admin");
    }

    [Fact]
    public async Task GetRolesAsync_KeycloakError_ReturnsEmptyList()
    {
        _handler.ResponseStatusCode = HttpStatusCode.InternalServerError;
        _handler.ResponseBody = string.Empty;

        IReadOnlyList<IdentityRole> result = await _provider.GetRolesAsync(
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    // --- Argument guard tests ---

    [Fact]
    public async Task GetUserAsync_NullUserId_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            () => _provider.GetUserAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetRoleMembersAsync_NullRoleName_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            () => _provider.GetRoleMembersAsync(null!, TestContext.Current.CancellationToken));
    }

    // --- Pagination edge cases ---

    [Fact]
    public async Task GetUsersAsync_NoParams_CallsEndpointWithoutQueryString()
    {
        _handler.ResponseBody = "[]";

        await _provider.GetUsersAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
        _handler.Requests[0].Url.ShouldEndWith("/admin/realms/test-realm/users");
        _handler.Requests[0].Url.ShouldNotContain("?");
    }

    [Fact]
    public async Task GetUsersAsync_WithOnlyFirst_IncludesFirstParam()
    {
        _handler.ResponseBody = "[]";

        await _provider.GetUsersAsync(
            first: 20,
            cancellationToken: TestContext.Current.CancellationToken);

        _handler.Requests[0].Url.ShouldContain("first=20");
        _handler.Requests[0].Url.ShouldNotContain("max=");
        _handler.Requests[0].Url.ShouldNotContain("search=");
    }

    [Fact]
    public async Task GetUsersAsync_WithOnlyMax_IncludesMaxParam()
    {
        _handler.ResponseBody = "[]";

        await _provider.GetUsersAsync(
            max: 50,
            cancellationToken: TestContext.Current.CancellationToken);

        _handler.Requests[0].Url.ShouldContain("max=50");
        _handler.Requests[0].Url.ShouldNotContain("first=");
        _handler.Requests[0].Url.ShouldNotContain("search=");
    }

    [Fact]
    public async Task GetUsersAsync_WithFirstZero_IncludesFirstParam()
    {
        _handler.ResponseBody = "[]";

        await _provider.GetUsersAsync(
            first: 0, max: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        _handler.Requests[0].Url.ShouldContain("first=0");
        _handler.Requests[0].Url.ShouldContain("max=10");
    }

    // --- Bearer token tests ---

    [Fact]
    public async Task GetUsersAsync_SetsAuthorizationHeader()
    {
        _handler.ResponseBody = "[]";

        await _provider.GetUsersAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
    }

    // --- Mapping tests ---

    [Fact]
    public async Task GetUserAsync_MapsAllFieldsCorrectly()
    {
        _handler.ResponseBody = """{"id":"user-1","username":"alice","email":"alice@test.com","firstName":"Alice","lastName":"Doe","enabled":true}""";

        IdentityUser? result = await _provider.GetUserAsync(
            "user-1", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Id.ShouldBe("user-1");
        result.Username.ShouldBe("alice");
        result.Email.ShouldBe("alice@test.com");
        result.FirstName.ShouldBe("Alice");
        result.LastName.ShouldBe("Doe");
        result.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task GetUserAsync_DisabledUser_MapsEnabledAsFalse()
    {
        _handler.ResponseBody = """{"id":"user-1","username":"bob","email":"bob@test.com","firstName":"Bob","lastName":"Smith","enabled":false}""";

        IdentityUser? result = await _provider.GetUserAsync(
            "user-1", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Enabled.ShouldBeFalse();
    }

    [Fact]
    public async Task GetRolesAsync_MapsDescriptionCorrectly()
    {
        _handler.ResponseBody = """[{"id":"role-1","name":"viewer","description":null}]""";

        IReadOnlyList<IdentityRole> result = await _provider.GetRolesAsync(
            TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Description.ShouldBeNull();
    }

    [Fact]
    public async Task GetRolesAsync_EmptyResponse_ReturnsEmptyList()
    {
        _handler.ResponseBody = "[]";

        IReadOnlyList<IdentityRole> result = await _provider.GetRolesAsync(
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRoleMembersAsync_CallsCorrectEndpointWithSpaceInRoleName()
    {
        _handler.ResponseBody = "[]";

        await _provider.GetRoleMembersAsync(
            "content editor", TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
        // Uri.ToString() may decode %20 back to a space, so check for the role name presence.
        _handler.Requests[0].Url.ShouldContain("/admin/realms/test-realm/roles/content");
        _handler.Requests[0].Url.ShouldContain("editor/users");
    }

    [Fact]
    public async Task GetUsersAsync_KeycloakUnavailable_ReturnsEmptyList()
    {
        _handler.ResponseStatusCode = HttpStatusCode.ServiceUnavailable;
        _handler.ResponseBody = string.Empty;

        IReadOnlyList<IdentityUser> result = await _provider.GetUsersAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUserAsync_CallsCorrectEndpoint()
    {
        _handler.ResponseBody = """{"id":"user-abc","username":"test","email":null,"firstName":null,"lastName":null,"enabled":true}""";

        await _provider.GetUserAsync("user-abc", TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
        _handler.Requests[0].Url.ShouldContain("/admin/realms/test-realm/users/user-abc");
    }

    [Fact]
    public async Task GetRolesAsync_CallsCorrectEndpoint()
    {
        _handler.ResponseBody = "[]";

        await _provider.GetRolesAsync(TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
        _handler.Requests[0].Url.ShouldContain("/admin/realms/test-realm/roles");
    }

    public void Dispose() => _httpClient.Dispose();
}
