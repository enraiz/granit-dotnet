using Granit.Authentication.JwtBearer.BackChannelLogout;
using Granit.Authentication.JwtBearer.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Authentication.Keycloak.Tests.BackChannelLogout;

public sealed class BackChannelLogoutEndpointTests
{
    private readonly BackChannelLogoutTokenValidator _validator;
    private readonly IRevokedSessionStore _store = Substitute.For<IRevokedSessionStore>();
    private readonly IOptions<JwtBearerAuthOptions> _options;
    private readonly ILogger<BackChannelLogoutTokenValidator> _logger;

    public BackChannelLogoutEndpointTests()
    {
        JwtBearerAuthOptions authOptions = new()
        {
            Authority = "https://keycloak.test/realms/test",
            Audience = "test-client",
            RequireHttpsMetadata = false,
            BackChannelLogout = new BackChannelLogoutOptions
            {
                Enabled = true,
                SessionRevocationTtl = TimeSpan.FromHours(1),
            },
        };
        _options = Microsoft.Extensions.Options.Options.Create(authOptions);
        _logger = Substitute.For<ILogger<BackChannelLogoutTokenValidator>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _validator = Substitute.ForPartsOf<BackChannelLogoutTokenValidator>(_options, _logger);
    }

    [Fact]
    public async Task HandleAsync_ValidLogoutToken_Returns200()
    {
        // Arrange
        HttpRequest request = CreateFormRequest("logout_token", "valid-jwt-token");
        _validator.ValidateAsync("valid-jwt-token", Arg.Any<CancellationToken>())
            .Returns(new BackChannelLogoutResult(true, "session-123", "user-456", null));

        // Act
        IResult result = await BackChannelLogoutEndpoint.HandleAsync(
            request, _validator, _store, _options, _logger, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeOfType<Ok>();
    }

    [Fact]
    public async Task HandleAsync_ValidLogoutToken_RevokesSession()
    {
        // Arrange
        HttpRequest request = CreateFormRequest("logout_token", "valid-jwt-token");
        _validator.ValidateAsync("valid-jwt-token", Arg.Any<CancellationToken>())
            .Returns(new BackChannelLogoutResult(true, "session-123", "user-456", null));

        // Act
        await BackChannelLogoutEndpoint.HandleAsync(
            request, _validator, _store, _options, _logger, TestContext.Current.CancellationToken);

        // Assert — prefers sid over sub
        await _store.Received(1).RevokeSessionAsync(
            "session-123",
            TimeSpan.FromHours(1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidLogoutTokenWithSubOnly_RevokesSubject()
    {
        // Arrange
        HttpRequest request = CreateFormRequest("logout_token", "valid-jwt-token");
        _validator.ValidateAsync("valid-jwt-token", Arg.Any<CancellationToken>())
            .Returns(new BackChannelLogoutResult(true, null, "user-456", null));

        // Act
        await BackChannelLogoutEndpoint.HandleAsync(
            request, _validator, _store, _options, _logger, TestContext.Current.CancellationToken);

        // Assert — falls back to sub when sid is null
        await _store.Received(1).RevokeSessionAsync(
            "user-456",
            TimeSpan.FromHours(1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingLogoutToken_Returns400()
    {
        // Arrange
        HttpRequest request = CreateFormRequest("other_field", "value");

        // Act
        IResult result = await BackChannelLogoutEndpoint.HandleAsync(
            request, _validator, _store, _options, _logger, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeAssignableTo<IStatusCodeHttpResult>()!
            .StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task HandleAsync_InvalidLogoutToken_Returns400()
    {
        // Arrange
        HttpRequest request = CreateFormRequest("logout_token", "invalid-token");
        _validator.ValidateAsync("invalid-token", Arg.Any<CancellationToken>())
            .Returns(new BackChannelLogoutResult(false, null, null, "Token validation failed."));

        // Act
        IResult result = await BackChannelLogoutEndpoint.HandleAsync(
            request, _validator, _store, _options, _logger, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeAssignableTo<IStatusCodeHttpResult>()!
            .StatusCode.ShouldBe(400);
    }

    private static HttpRequest CreateFormRequest(string key, string value)
    {
        DefaultHttpContext context = new();
        FormCollection form = new(new Dictionary<string, StringValues>
        {
            [key] = value,
        });
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = form;
        return context.Request;
    }
}
