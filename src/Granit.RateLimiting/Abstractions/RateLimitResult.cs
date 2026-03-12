namespace Granit.RateLimiting.Abstractions;

/// <summary>
/// Result of a rate limit check.
/// </summary>
/// <param name="IsAllowed">Whether the request is within the quota.</param>
/// <param name="Remaining">Number of permits remaining in the current window.</param>
/// <param name="Limit">Total permit limit for the policy.</param>
/// <param name="RetryAfter">Time to wait before retrying. <see cref="TimeSpan.Zero"/> when allowed.</param>
public sealed record RateLimitResult(bool IsAllowed, int Remaining, int Limit, TimeSpan RetryAfter);
