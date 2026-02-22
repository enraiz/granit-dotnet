// =============================================================================
// SettingProvider - Cascading setting resolution
// =============================================================================
// Walks the providers in order U(100) → T(200) → G(300) → C(400) → D(500).
// Returns the first non-null value found.
//
// Cascade rules:
//   - If SettingDefinition.Providers is non-empty, only the listed providers are consulted.
//   - If SettingDefinition.IsInherited = false, the cascade stops at the first applicable
//     provider that returns null (no fallback to lower-priority levels).
//
// Inputs  : SettingValueProviderManager, SettingDefinitionManager
// Outputs : string? (resolved value) or IReadOnlyList<SettingValue>
// =============================================================================

using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Providers;
using DigitalDynamics.Foundation.Settings.Values;

namespace DigitalDynamics.Foundation.Settings.Services;

/// <summary>
/// Implementation of <see cref="ISettingProvider"/> with cascading resolution.
/// </summary>
public sealed class SettingProvider : ISettingProvider
{
    private readonly SettingValueProviderManager _providerManager;
    private readonly SettingDefinitionManager _definitionManager;

    public SettingProvider(
        SettingValueProviderManager providerManager,
        SettingDefinitionManager definitionManager)
    {
        _providerManager = providerManager;
        _definitionManager = definitionManager;
    }

    /// <inheritdoc/>
    public async Task<string?> GetOrNullAsync(string name, CancellationToken ct = default)
    {
        SettingValue? resolved = await ResolveAsync(name, ct);
        return resolved?.Value;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SettingValue>> GetAllAsync(string[] names, CancellationToken ct = default)
    {
        List<SettingValue> result = new(names.Length);
        foreach (string name in names)
        {
            SettingValue? resolved = await ResolveAsync(name, ct);
            result.Add(resolved ?? new SettingValue(name, string.Empty, null, null));
        }
        return result;
    }

    private async Task<SettingValue?> ResolveAsync(string name, CancellationToken ct)
    {
        SettingDefinition? definition = _definitionManager.GetOrNull(name);
        if (definition is null)
        {
            return null;
        }

        bool hasAllowList = definition.Providers.Count > 0;

        foreach (ISettingValueProvider provider in _providerManager.Providers)
        {
            if (hasAllowList && !definition.Providers.Contains(provider.Name))
            {
                continue;
            }

            SettingValue? value = await provider.GetOrNullAsync(definition, ct);
            if (value is not null)
            {
                return value;
            }

            // Without inheritance, do not fall back to lower-priority levels
            if (!definition.IsInherited)
            {
                return null;
            }
        }

        return null;
    }
}
