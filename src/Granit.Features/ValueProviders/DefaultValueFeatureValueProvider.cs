using Granit.Features.Definitions;

namespace Granit.Features.ValueProviders;

/// <summary>
/// Fallback provider — always returns the feature's declared default value.
/// Runs last in the cascade (order = 300).
/// </summary>
internal sealed class DefaultValueFeatureValueProvider : IFeatureValueProvider
{
    /// <inheritdoc/>
    public string Name => "Default";

    /// <inheritdoc/>
    public int Order => 300;

    /// <inheritdoc/>
    public Task<string?> GetOrNullAsync(FeatureDefinition definition, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(definition.DefaultValue);
}
