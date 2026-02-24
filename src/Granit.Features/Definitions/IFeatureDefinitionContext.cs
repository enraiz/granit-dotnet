namespace Granit.Features.Definitions;

/// <summary>
/// Provides the API for declaring feature groups and features inside a
/// <see cref="IFeatureDefinitionProvider.Define"/> implementation.
/// </summary>
public interface IFeatureDefinitionContext
{
    /// <summary>
    /// Creates a new feature group and adds it to the definition context.
    /// </summary>
    /// <param name="name">Group name (e.g. <c>"Guava"</c>).</param>
    /// <param name="displayName">Optional display label for admin UI.</param>
    /// <returns>The newly created <see cref="FeatureGroupDefinition"/> for fluent chaining.</returns>
    FeatureGroupDefinition AddGroup(string name, string? displayName = null);
}
