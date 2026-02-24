namespace Granit.Features.Definitions;

/// <summary>
/// Read-only registry of all feature definitions declared at application startup.
/// </summary>
/// <remarks>
/// Built once (singleton) by aggregating all registered
/// <see cref="IFeatureDefinitionProvider"/> instances. Immutable after startup.
/// </remarks>
public interface IFeatureDefinitionStore
{
    /// <summary>
    /// Returns the definition for <paramref name="featureName"/>.
    /// </summary>
    /// <exception cref="Exceptions.FeatureNotFoundException">If the feature is not declared.</exception>
    FeatureDefinition GetRequired(string featureName);

    /// <summary>Returns the definition, or <c>null</c> if not declared.</summary>
    FeatureDefinition? GetOrNull(string featureName);

    /// <summary>Returns all declared feature definitions.</summary>
    IReadOnlyList<FeatureDefinition> GetAll();
}
