using Granit.Settings.Definitions;
using Granit.Settings.Providers;
using Granit.Settings.Values;

namespace Granit.Settings.Services;

/// <summary>
/// Implementation of <see cref="ISettingProvider"/> with cascading resolution.
/// </summary>
public sealed class SettingProvider(
    SettingValueProviderManager providerManager,
    SettingDefinitionManager definitionManager) : ISettingProvider
{
    private readonly SettingValueProviderManager _providerManager = providerManager;
    private readonly SettingDefinitionManager _definitionManager = definitionManager;

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
