namespace Granit.Features.Exceptions;

/// <summary>
/// Exception thrown when a feature name is referenced in code but not declared
/// in any registered <see cref="Definitions.IFeatureDefinitionProvider"/>.
/// </summary>
/// <remarks>
/// This is a programming error, not a user-facing error.
/// It indicates that a feature constant references a name that was never defined.
/// </remarks>
public sealed class FeatureNotFoundException : Exception
{
    /// <summary>The name of the missing feature.</summary>
    public string FeatureName { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="FeatureNotFoundException"/>.
    /// </summary>
    /// <param name="featureName">The name of the feature that was not found.</param>
    public FeatureNotFoundException(string featureName)
        : base($"Feature '{featureName}' is not declared in any IFeatureDefinitionProvider.")
    {
        FeatureName = featureName;
    }
}
