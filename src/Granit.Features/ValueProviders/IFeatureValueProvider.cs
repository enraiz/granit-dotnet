using Granit.Features.Definitions;

namespace Granit.Features.ValueProviders;

/// <summary>
/// Resolves a feature value for a single level of the resolution cascade.
/// </summary>
/// <remarks>
/// Providers are iterated in ascending <see cref="Order"/> — the first to return a
/// non-<c>null</c> value wins. Built-in order:
/// <list type="table">
///   <item><term>Tenant (100)</term><description>Tenant-specific override.</description></item>
///   <item><term>Plan (200)</term><description>Commercial plan override.</description></item>
///   <item><term>Default (300)</term><description>Declared default value.</description></item>
/// </list>
/// </remarks>
public interface IFeatureValueProvider
{
    /// <summary>Short provider identifier (e.g., <c>"Tenant"</c>, <c>"Plan"</c>, <c>"Default"</c>).</summary>
    string Name { get; }

    /// <summary>
    /// Resolution order — lower value = higher priority.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Returns the resolved value for <paramref name="definition"/> in the current context,
    /// or <c>null</c> to defer to the next provider.
    /// </summary>
    Task<string?> GetOrNullAsync(FeatureDefinition definition, CancellationToken cancellationToken = default);
}
