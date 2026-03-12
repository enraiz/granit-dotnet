namespace Granit.RateLimiting.Abstractions;

/// <summary>
/// Resolves the effective permit limit for a given rate limiting policy.
/// Implementations may read from static configuration or dynamic sources (e.g., tenant plan features).
/// </summary>
public interface IRateLimitQuotaProvider
{
    /// <summary>
    /// Returns the effective permit limit for <paramref name="policyName"/> in the current context.
    /// </summary>
    /// <param name="policyName">Name of the rate limiting policy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The permit limit, or <see langword="null"/> if the policy is not configured.</returns>
    Task<int?> GetPermitLimitAsync(string policyName, CancellationToken cancellationToken = default);
}
