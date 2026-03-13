using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Granit.BlobStorage.Proxy.Internal;

/// <summary>
/// <see cref="IDistributedCache"/>-backed token store with single-use semantics.
/// Tokens are 128-bit cryptographic nonces encoded as base64url (22 characters).
/// </summary>
internal sealed class DistributedBlobProxyTokenStore(IDistributedCache cache) : IBlobProxyTokenStore
{
    private const string KeyPrefix = "granit:blob-proxy:";
    private const int TokenByteLength = 16; // 128-bit nonce

    /// <inheritdoc/>
    public async Task<string> CreateAsync(
        ProxyTokenEntry entry,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        string token = GenerateToken();
        string cacheKey = BuildKey(token);

        byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(entry);

        DistributedCacheEntryOptions cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = ttl,
        };

        await cache.SetAsync(cacheKey, serialized, cacheOptions, cancellationToken).ConfigureAwait(false);

        return token;
    }

    /// <inheritdoc/>
    public async Task<ProxyTokenEntry?> ConsumeAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = BuildKey(token);

        byte[]? bytes = await cache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
        {
            return null;
        }

        // Remove immediately — single-use.
        await cache.RemoveAsync(cacheKey, cancellationToken).ConfigureAwait(false);

        return JsonSerializer.Deserialize<ProxyTokenEntry>(bytes);
    }

    private static string GenerateToken()
    {
        Span<byte> buffer = stackalloc byte[TokenByteLength];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string BuildKey(string token) => $"{KeyPrefix}{token}";
}
