using Granit.Authentication.JwtBearer.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.Authentication.JwtBearer.BackChannelLogout;

/// <summary>
/// Minimal API handler for the OIDC back-channel logout endpoint.
/// Receives a <c>logout_token</c> via <c>application/x-www-form-urlencoded</c> POST
/// and revokes the session in the distributed cache.
/// </summary>
internal static partial class BackChannelLogoutEndpoint
{
    /// <summary>
    /// Handles the back-channel logout POST request from the identity provider.
    /// </summary>
    public static async Task<IResult> HandleAsync(
        HttpRequest request,
        BackChannelLogoutTokenValidator validator,
        IRevokedSessionStore store,
        IOptions<JwtBearerAuthOptions> options,
        ILogger<BackChannelLogoutTokenValidator> logger,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            LogInvalidContentType(logger);
            return Results.BadRequest(new { error = "Expected application/x-www-form-urlencoded content type." });
        }

        IFormCollection form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        string logoutToken = form["logout_token"].ToString();

        if (string.IsNullOrEmpty(logoutToken))
        {
            LogMissingLogoutToken(logger);
            return Results.BadRequest(new { error = "Missing 'logout_token' form parameter." });
        }

        BackChannelLogoutResult result = await validator.ValidateAsync(logoutToken, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
        {
            return Results.BadRequest(new { error = result.Error });
        }

        TimeSpan ttl = options.Value.BackChannelLogout.SessionRevocationTtl;

        // Prefer sid (session-specific), fall back to sub (user-wide)
        string sessionKey = result.SessionId ?? result.SubjectId!;
        await store.RevokeSessionAsync(sessionKey, ttl, cancellationToken).ConfigureAwait(false);

        return Results.Ok();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Back-channel logout request rejected: invalid content type.")]
    private static partial void LogInvalidContentType(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Back-channel logout request rejected: missing 'logout_token' parameter.")]
    private static partial void LogMissingLogoutToken(ILogger logger);
}
