namespace Granit.Authentication.JwtBearer.BackChannelLogout;

/// <summary>
/// Stores and queries revoked session identifiers for OIDC back-channel logout.
/// </summary>
public interface IRevokedSessionStore
{
    /// <summary>
    /// Marks a session as revoked for the given time-to-live.
    /// </summary>
    /// <param name="sessionId">The session identifier (<c>sid</c> or <c>sub</c> from the logout token).</param>
    /// <param name="ttl">How long the revocation entry should be kept.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeSessionAsync(string sessionId, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a session has been revoked.
    /// </summary>
    /// <param name="sessionId">The session identifier (<c>sid</c> or <c>sub</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the session is revoked; otherwise <c>false</c>.</returns>
    Task<bool> IsSessionRevokedAsync(string sessionId, CancellationToken cancellationToken = default);
}
