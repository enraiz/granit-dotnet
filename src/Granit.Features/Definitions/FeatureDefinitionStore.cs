using Granit.Features.Exceptions;

namespace Granit.Features.Definitions;

/// <summary>
/// Singleton registry built at startup from all registered
/// <see cref="IFeatureDefinitionProvider"/> instances.
/// </summary>
internal sealed class FeatureDefinitionStore : IFeatureDefinitionStore
{
    private readonly IReadOnlyDictionary<string, FeatureDefinition> _definitions;

    public FeatureDefinitionStore(IEnumerable<IFeatureDefinitionProvider> providers)
    {
        FeatureDefinitionContext context = new();
        foreach (IFeatureDefinitionProvider provider in providers)
        {
            provider.Define(context);
        }

        _definitions = context.GetAllDefinitions()
            .ToDictionary(d => d.Name, StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public FeatureDefinition GetRequired(string featureName) =>
        _definitions.TryGetValue(featureName, out FeatureDefinition? def)
            ? def
            : throw new FeatureNotFoundException(featureName);

    /// <inheritdoc/>
    public FeatureDefinition? GetOrNull(string featureName) =>
        _definitions.GetValueOrDefault(featureName);

    /// <inheritdoc/>
    public IReadOnlyList<FeatureDefinition> GetAll() => [.. _definitions.Values];
}
