namespace Granit.BlobStorage.Proxy.Internal;

/// <summary>
/// Manages ephemeral, single-use tokens for proxy upload/download endpoints.
/// </summary>
internal interface IBlobProxyTokenStore
{
    /// <summary>
    /// Creates a new token bound to the given <paramref name="entry"/> with the specified TTL.
    /// </summary>
    /// <returns>A cryptographically random, URL-safe token string.</returns>
    Task<string> CreateAsync(ProxyTokenEntry entry, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically retrieves and deletes the token entry (single-use semantics).
    /// Returns <c>null</c> if the token does not exist or has expired.
    /// </summary>
    Task<ProxyTokenEntry?> ConsumeAsync(string token, CancellationToken cancellationToken = default);
}
