namespace Granit.Features;

/// <summary>
/// Resolves the effective value of a feature for the current request context,
/// walking the Tenant → Plan → Default cascade with hybrid caching.
/// </summary>
public interface IFeatureChecker
{
    /// <summary>
    /// Returns <c>true</c> when the feature is enabled for the current tenant/plan context
    /// (<c>Toggle</c> feature with resolved value <c>"true"</c>).
    /// </summary>
    /// <exception cref="Exceptions.FeatureNotFoundException">If the feature is not declared.</exception>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the resolved numeric value for a <c>Numeric</c> feature.
    /// Returns <c>0</c> if the stored value cannot be parsed as <see cref="long"/>.
    /// </summary>
    /// <exception cref="Exceptions.FeatureNotFoundException">If the feature is not declared.</exception>
    Task<long> GetNumericAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the raw resolved string value for a feature.
    /// </summary>
    /// <exception cref="Exceptions.FeatureNotFoundException">If the feature is not declared.</exception>
    Task<string> GetValueAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asserts that the feature is enabled; throws
    /// <see cref="Exceptions.FeatureNotEnabledException"/> (HTTP 403) otherwise.
    /// </summary>
    /// <exception cref="Exceptions.FeatureNotFoundException">If the feature is not declared.</exception>
    /// <exception cref="Exceptions.FeatureNotEnabledException">If the feature is disabled.</exception>
    Task RequireEnabledAsync(string featureName, CancellationToken cancellationToken = default);
}
