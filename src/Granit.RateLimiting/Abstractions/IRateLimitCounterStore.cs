using Granit.RateLimiting.Options;

namespace Granit.RateLimiting.Abstractions;

/// <summary>
/// Atomic counter store for rate limiting. Implementations must guarantee atomicity
/// across concurrent calls (e.g., Redis Lua scripts).
/// </summary>
public interface IRateLimitCounterStore
{
    /// <summary>
    /// Atomically checks the current counter and increments it if within the quota.
    /// </summary>
    /// <param name="key">Composite key (prefix + tenant + policy).</param>
    /// <param name="permitLimit">Maximum number of permits in the window.</param>
    /// <param name="window">Duration of the rate limiting window.</param>
    /// <param name="algorithm">Algorithm to apply.</param>
    /// <param name="policyOptions">Full policy options for algorithm-specific parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating whether the request is allowed and remaining quota.</returns>
    Task<RateLimitResult> CheckAndIncrementAsync(
        string key,
        int permitLimit,
        TimeSpan window,
        RateLimitAlgorithm algorithm,
        RateLimitPolicyOptions policyOptions,
        CancellationToken cancellationToken);
}
