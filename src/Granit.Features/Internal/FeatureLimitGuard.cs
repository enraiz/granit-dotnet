using Granit.Features.Exceptions;

namespace Granit.Features.Internal;

/// <summary>
/// Enforces numeric feature limits using the resolved value from <see cref="IFeatureChecker"/>.
/// </summary>
internal sealed class FeatureLimitGuard(IFeatureChecker featureChecker) : IFeatureLimitGuard
{
    private readonly IFeatureChecker _featureChecker = featureChecker;

    /// <inheritdoc/>
    public async Task CheckAsync(string featureName, long currentCount, CancellationToken cancellationToken = default)
    {
        long limit = await _featureChecker.GetNumericAsync(featureName, cancellationToken).ConfigureAwait(false);
        if (currentCount >= limit)
        {
            throw new FeatureLimitExceededException(featureName, currentCount, limit);
        }
    }

    /// <inheritdoc/>
    public Task<long> GetLimitAsync(string featureName, CancellationToken cancellationToken = default) =>
        _featureChecker.GetNumericAsync(featureName, cancellationToken);
}
