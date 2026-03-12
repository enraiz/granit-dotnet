using Granit.RateLimiting.Abstractions;
using Granit.RateLimiting.Options;
using Microsoft.Extensions.Options;

namespace Granit.RateLimiting.Internal;

/// <summary>
/// Resolves rate limit quotas from <c>Granit.Features</c> Numeric features.
/// Convention: feature name = <c>RateLimit.{PolicyName}</c> (overridable via <see cref="RateLimitPolicyOptions.FeatureName"/>).
/// Falls back to <see cref="OptionsRateLimitQuotaProvider"/> when the feature is not defined or
/// <c>IFeatureChecker</c> is not registered.
/// </summary>
internal sealed class FeatureBasedRateLimitQuotaProvider(
    IOptionsMonitor<GranitRateLimitingOptions> options,
    IServiceProvider serviceProvider) : IRateLimitQuotaProvider
{
    /// <inheritdoc/>
    public async Task<int?> GetPermitLimitAsync(string policyName, CancellationToken cancellationToken = default)
    {
        if (!options.CurrentValue.Policies.TryGetValue(policyName, out RateLimitPolicyOptions? policy))
        {
            return null;
        }

        // Resolve IFeatureChecker without compile-time coupling to the concrete type.
        // This allows the module to work without Granit.Features installed.
        var featureChecker = serviceProvider.GetService(typeof(Features.IFeatureChecker)) as Features.IFeatureChecker;

        if (featureChecker is not null)
        {
            string featureName = policy.FeatureName ?? $"RateLimit.{policyName}";

            try
            {
                long value = await featureChecker.GetNumericAsync(featureName, cancellationToken).ConfigureAwait(false);
                if (value > 0)
                {
                    return (int)value;
                }
            }
            catch (Features.Exceptions.FeatureNotFoundException)
            {
                // Feature not defined — fall through to static config.
            }
        }

        return policy.PermitLimit;
    }
}
