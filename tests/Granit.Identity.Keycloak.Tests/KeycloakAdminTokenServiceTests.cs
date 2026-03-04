using Granit.Identity.Keycloak.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Identity.Keycloak.Tests;

public sealed class KeycloakAdminTokenServiceTests : IDisposable
{
    private readonly KeycloakAdminOptions _options = new()
    {
        BaseUrl = "https://keycloak.test",
        Realm = "test-realm",
        ClientId = "admin-service",
        ClientSecret = "secret",
    };

    private readonly MockHttpMessageHandler _handler = new();
    private readonly KeycloakAdminTokenService _service;

    public KeycloakAdminTokenServiceTests()
    {
        _handler.ResponseBody = """{"access_token":"test-token","expires_in":300}""";

        HttpClient client = new(_handler) { BaseAddress = new Uri("https://keycloak.test/") };
        IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("KeycloakAdmin").Returns(client);

        _service = new KeycloakAdminTokenService(
            factory,
            Options.Create(_options),
            NullLogger<KeycloakAdminTokenService>.Instance);
    }

    [Fact]
    public async Task GetTokenAsync_ReturnsAccessToken()
    {
        string token = await _service.GetTokenAsync(TestContext.Current.CancellationToken);

        token.ShouldBe("test-token");
        _handler.Requests.Count.ShouldBe(1);
        _handler.Requests[0].Url.ShouldContain("/realms/test-realm/protocol/openid-connect/token");
    }

    [Fact]
    public async Task GetTokenAsync_CachesToken_DoesNotCallTwice()
    {
        string token1 = await _service.GetTokenAsync(TestContext.Current.CancellationToken);
        string token2 = await _service.GetTokenAsync(TestContext.Current.CancellationToken);

        token1.ShouldBe("test-token");
        token2.ShouldBe("test-token");
        _handler.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetTokenAsync_SendsClientCredentialsGrant()
    {
        await _service.GetTokenAsync(TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
        string body = _handler.Requests[0].Body;
        body.ShouldContain("grant_type=client_credentials");
        body.ShouldContain("client_id=admin-service");
        body.ShouldContain("client_secret=secret");
    }

    public void Dispose() => _service.Dispose();
}
