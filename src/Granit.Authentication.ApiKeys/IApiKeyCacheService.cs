using Granit.Authentication.ApiKeys.Domain;

namespace Granit.Authentication.ApiKeys;

/// <summary>
/// Abstraction for caching API key lookups. Decouples the authentication handler
/// from the <c>Granit.Caching</c> package (soft dependency).
/// </summary>
/// <remarks>
/// When <c>Granit.Caching</c> is not installed, no implementation is registered
/// and the handler falls back to direct store lookups.
/// </remarks>
public interface IApiKeyCacheService
{
    /// <summary>
    /// Gets the API key from cache or loads it from the store if not cached.
    /// </summary>
    /// <param name="hashedKey">The SHA-256 hash of the API key (used as cache key).</param>
    /// <param name="factory">Factory to load from the store on cache miss.</param>
    /// <param name="duration">Cache TTL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ApiKeyEntry?> GetOrLoadAsync(
        string hashedKey,
        Func<CancellationToken, Task<ApiKeyEntry?>> factory,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached API key entry (e.g., on revocation).
    /// </summary>
    Task RemoveAsync(string hashedKey, CancellationToken cancellationToken = default);
}
