namespace Granit.Authentication.Keycloak.BackChannelLogout;

/// <summary>
/// Stores and queries revoked session identifiers for back-channel logout.
/// </summary>
public interface IRevokedSessionStore
{
    /// <summary>
    /// Marks a session as revoked for the given time-to-live.
    /// </summary>
    /// <param name="sessionId">The Keycloak session identifier (<c>sid</c> or <c>sub</c>).</param>
    /// <param name="ttl">How long the revocation entry should be kept.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeSessionAsync(string sessionId, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a session has been revoked.
    /// </summary>
    /// <param name="sessionId">The Keycloak session identifier (<c>sid</c> or <c>sub</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the session is revoked; otherwise <c>false</c>.</returns>
    Task<bool> IsSessionRevokedAsync(string sessionId, CancellationToken cancellationToken = default);
}
