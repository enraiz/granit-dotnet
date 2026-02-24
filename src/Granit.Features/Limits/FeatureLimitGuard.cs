using Granit.Features.Checker;
using Granit.Features.Exceptions;

namespace Granit.Features.Limits;

/// <summary>
/// Enforces numeric feature limits using the resolved value from <see cref="IFeatureChecker"/>.
/// </summary>
internal sealed class FeatureLimitGuard(IFeatureChecker featureChecker) : IFeatureLimitGuard
{
    private readonly IFeatureChecker _featureChecker = featureChecker;

    /// <inheritdoc/>
    public async Task CheckAsync(string featureName, long currentCount, CancellationToken ct = default)
    {
        long limit = await _featureChecker.GetNumericAsync(featureName, ct);
        if (currentCount >= limit)
        {
            throw new FeatureLimitExceededException(featureName, currentCount, limit);
        }
    }

    /// <inheritdoc/>
    public Task<long> GetLimitAsync(string featureName, CancellationToken ct = default) =>
        _featureChecker.GetNumericAsync(featureName, ct);
}
