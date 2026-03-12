using Granit.RateLimiting.Abstractions;
using Granit.RateLimiting.Options;
using Microsoft.Extensions.Options;

namespace Granit.RateLimiting.Internal;

/// <summary>
/// Resolves rate limit quotas from static <see cref="GranitRateLimitingOptions"/> configuration.
/// </summary>
internal sealed class OptionsRateLimitQuotaProvider(
    IOptionsMonitor<GranitRateLimitingOptions> options) : IRateLimitQuotaProvider
{
    /// <inheritdoc/>
    public Task<int?> GetPermitLimitAsync(string policyName, CancellationToken cancellationToken = default)
    {
        int? result = options.CurrentValue.Policies.TryGetValue(policyName, out RateLimitPolicyOptions? policy)
            ? policy.PermitLimit
            : null;

        return Task.FromResult(result);
    }
}
