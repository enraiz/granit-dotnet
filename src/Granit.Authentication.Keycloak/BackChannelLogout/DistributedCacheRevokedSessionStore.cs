using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Granit.Authentication.Keycloak.BackChannelLogout;

/// <summary>
/// <see cref="IRevokedSessionStore"/> backed by <see cref="IDistributedCache"/>.
/// Each revoked session is stored as a simple existence marker with a TTL.
/// </summary>
internal sealed partial class DistributedCacheRevokedSessionStore(
    IDistributedCache cache,
    ILogger<DistributedCacheRevokedSessionStore> logger) : IRevokedSessionStore
{
    private const string KeyPrefix = "granit:revoked-session:";
    private static readonly byte[] Marker = [1];

    /// <inheritdoc/>
    public async Task RevokeSessionAsync(string sessionId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        string key = KeyPrefix + sessionId;
        DistributedCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = ttl,
        };

        await cache.SetAsync(key, Marker, options, cancellationToken).ConfigureAwait(false);
        LogSessionRevoked(logger, sessionId, ttl);
    }

    /// <inheritdoc/>
    public async Task<bool> IsSessionRevokedAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        string key = KeyPrefix + sessionId;
        byte[]? data = await cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        return data is not null;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Session '{SessionId}' revoked via back-channel logout (TTL: {Ttl}).")]
    private static partial void LogSessionRevoked(ILogger logger, string sessionId, TimeSpan ttl);
}
