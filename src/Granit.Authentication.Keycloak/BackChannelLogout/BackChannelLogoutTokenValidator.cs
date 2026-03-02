using System.Text.Json;
using Granit.Authentication.Keycloak.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Granit.Authentication.Keycloak.BackChannelLogout;

/// <summary>
/// Validates Keycloak back-channel <c>logout_token</c> JWTs per the
/// OIDC Back-Channel Logout 1.0 specification.
/// Not sealed — virtual method required for test substitution (NSubstitute).
/// </summary>
#pragma warning disable CA1852
internal partial class BackChannelLogoutTokenValidator
#pragma warning restore CA1852
{
    private const string BackChannelLogoutEvent = "http://schemas.openid.net/event/backchannel-logout";

    private readonly IOptions<KeycloakOptions> _options;
    private readonly JsonWebTokenHandler _tokenHandler;
    private readonly ILogger<BackChannelLogoutTokenValidator> _logger;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

    public BackChannelLogoutTokenValidator(
        IOptions<KeycloakOptions> options,
        ILogger<BackChannelLogoutTokenValidator> logger)
    {
        _options = options;
        _logger = logger;
        _tokenHandler = new JsonWebTokenHandler();

        string authority = options.Value.Authority.TrimEnd('/');
        string metadataAddress = $"{authority}/.well-known/openid-configuration";
        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = options.Value.RequireHttpsMetadata });
    }

    /// <summary>
    /// Validates the <paramref name="logoutToken"/> and extracts session/subject identifiers.
    /// </summary>
    /// <param name="logoutToken">The raw <c>logout_token</c> JWT string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="BackChannelLogoutResult"/> indicating success or failure.</returns>
    public virtual async Task<BackChannelLogoutResult> ValidateAsync(string logoutToken, CancellationToken cancellationToken = default)
    {
        KeycloakOptions keycloakOptions = _options.Value;
        string audience = keycloakOptions.Audience ?? keycloakOptions.ClientId;

        OpenIdConnectConfiguration config;
        try
        {
            config = await _configurationManager.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogMetadataFetchFailed(_logger, ex);
            return new BackChannelLogoutResult(false, null, null, "Failed to fetch OIDC metadata.");
        }

        TokenValidationParameters validationParameters = new()
        {
            ValidIssuer = keycloakOptions.Authority,
            ValidAudience = audience,
            IssuerSigningKeys = config.SigningKeys,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // logout_token has no exp requirement per spec
            ValidateIssuerSigningKey = true,
        };

        TokenValidationResult result = await _tokenHandler.ValidateTokenAsync(logoutToken, validationParameters).ConfigureAwait(false);
        if (!result.IsValid)
        {
            LogTokenValidationFailed(_logger, result.Exception);
            return new BackChannelLogoutResult(false, null, null, $"Token validation failed: {result.Exception.Message}");
        }

        // Verify the "events" claim contains the back-channel logout event
        if (!result.Claims.TryGetValue("events", out var eventsValue) || !ContainsBackChannelLogoutEvent(eventsValue))
        {
            LogMissingBackChannelEvent(_logger);
            return new BackChannelLogoutResult(false, null, null, "Missing or invalid 'events' claim.");
        }

        // Extract sid (preferred) and/or sub
        result.Claims.TryGetValue("sid", out var sidValue);
        result.Claims.TryGetValue("sub", out var subValue);

        string? sessionId = sidValue?.ToString();
        string? subjectId = subValue?.ToString();

        if (string.IsNullOrEmpty(sessionId) && string.IsNullOrEmpty(subjectId))
        {
            LogMissingSessionIdentifier(_logger);
            return new BackChannelLogoutResult(false, null, null, "Token must contain either 'sid' or 'sub' claim.");
        }

        LogTokenValidated(_logger, sessionId, subjectId);
        return new BackChannelLogoutResult(true, sessionId, subjectId, null);
    }

    private static bool ContainsBackChannelLogoutEvent(object eventsValue)
    {
        // The events claim can be a JSON string (from JWT parsing) or a JsonElement
        if (eventsValue is string eventsString)
        {
            return eventsString.Contains(BackChannelLogoutEvent, StringComparison.Ordinal);
        }

        if (eventsValue is JsonElement element)
        {
            return element.ValueKind == JsonValueKind.Object
                   && element.TryGetProperty(BackChannelLogoutEvent, out _);
        }

        return false;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to fetch OIDC metadata for back-channel logout token validation.")]
    private static partial void LogMetadataFetchFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Back-channel logout token validation failed.")]
    private static partial void LogTokenValidationFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Back-channel logout token missing required 'events' claim with back-channel logout event.")]
    private static partial void LogMissingBackChannelEvent(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Back-channel logout token missing both 'sid' and 'sub' claims.")]
    private static partial void LogMissingSessionIdentifier(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Back-channel logout token validated (sid: {SessionId}, sub: {SubjectId}).")]
    private static partial void LogTokenValidated(ILogger logger, string? sessionId, string? subjectId);
}
