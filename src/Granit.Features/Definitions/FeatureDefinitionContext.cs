namespace Granit.Features.Definitions;

/// <summary>
/// Internal implementation of <see cref="IFeatureDefinitionContext"/>.
/// Collects all feature groups and definitions during module startup.
/// </summary>
internal sealed class FeatureDefinitionContext : IFeatureDefinitionContext
{
    private readonly List<FeatureGroupDefinition> _groups = [];

    /// <inheritdoc/>
    public FeatureGroupDefinition AddGroup(string name, string? displayName = null)
    {
        FeatureGroupDefinition group = new(name, displayName);
        _groups.Add(group);
        return group;
    }

    /// <summary>
    /// Returns a flattened sequence of all declared <see cref="FeatureDefinition"/> instances.
    /// </summary>
    internal IEnumerable<FeatureDefinition> GetAllDefinitions() =>
        _groups.SelectMany(g => g.Features);
}
