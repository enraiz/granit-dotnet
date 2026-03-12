using Granit.Core.MultiTenancy;
using Granit.RateLimiting.Abstractions;
using Granit.RateLimiting.Options;
using Granit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granit.RateLimiting.Internal;

/// <summary>
/// Core rate limiting logic shared between the ASP.NET Core endpoint filter and Wolverine behavior.
/// Partitions by tenant, resolves quotas dynamically, checks bypass roles, and records metrics.
/// </summary>
/// <remarks>
/// Public so that Wolverine can resolve it from the DI container via parameter injection.
/// </remarks>
public sealed class TenantPartitionedRateLimiter(
    IRateLimitCounterStore counterStore,
    IRateLimitQuotaProvider quotaProvider,
    IOptions<GranitRateLimitingOptions> options,
    ICurrentTenant currentTenant,
    ICurrentUserService currentUser,
    RateLimitingMetrics metrics,
    ILogger<TenantPartitionedRateLimiter> logger)
{
    private readonly GranitRateLimitingOptions _options = options.Value;

    /// <summary>
    /// Checks whether the current request is within the rate limit for <paramref name="policyName"/>.
    /// </summary>
    /// <returns>The rate limit result, or <see langword="null"/> if the policy is not configured.</returns>
    public async Task<RateLimitResult?> CheckAsync(string policyName, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        if (!_options.Policies.TryGetValue(policyName, out RateLimitPolicyOptions? policy))
        {
            return null;
        }

        // Check bypass claims
        if (TryBypass(policyName))
        {
            return null;
        }

        string? tenantId = currentTenant.IsAvailable ? currentTenant.Id?.ToString() : null;
        string key = BuildKey(policyName, tenantId);

        int permitLimit = await quotaProvider.GetPermitLimitAsync(policyName, cancellationToken).ConfigureAwait(false)
                          ?? policy.PermitLimit;

        RateLimitResult result = await counterStore.CheckAndIncrementAsync(
            key, permitLimit, policy.Window, policy.Algorithm, policy, cancellationToken).ConfigureAwait(false);

        if (result.IsAllowed)
        {
            metrics.RecordAllowed(policyName, tenantId);
            RateLimitingLog.LogRateLimitChecked(logger, policyName, tenantId, result.Remaining, result.Limit);
        }
        else
        {
            metrics.RecordRejected(policyName, tenantId);
            RateLimitingLog.LogRateLimitExceeded(logger, policyName, tenantId, result.Remaining, result.RetryAfter.TotalSeconds);
        }

        return result;
    }

    private bool TryBypass(string policyName)
    {
        if (_options.BypassRoles.Length == 0 || !currentUser.IsAuthenticated)
        {
            return false;
        }

        foreach (string role in _options.BypassRoles)
        {
            if (currentUser.IsInRole(role))
            {
                RateLimitingLog.LogBypassApplied(logger, policyName, role, currentUser.UserId);
                return true;
            }
        }

        return false;
    }

    private string BuildKey(string policyName, string? tenantId)
    {
        string segment = tenantId ?? "global";
        // Hash tag {segment} ensures all keys for a tenant hash to the same Redis Cluster slot.
        return $"{_options.KeyPrefix}:{{{segment}}}:{policyName}";
    }
}
