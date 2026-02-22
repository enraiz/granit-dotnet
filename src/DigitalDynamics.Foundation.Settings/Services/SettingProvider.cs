// =============================================================================
// SettingProvider - Résolution en cascade des paramètres
// =============================================================================
// Parcourt les providers dans l'ordre U(100) → T(200) → G(300) → C(400) → D(500).
// Retourne la première valeur non nulle trouvée.
//
// Règles de cascade :
//   - Si SettingDefinition.Providers est non vide, seuls les providers listés sont consultés.
//   - Si SettingDefinition.IsInherited = false, la cascade s'arrête au premier provider
//     applicable qui retourne null (pas de fallback vers les niveaux inférieurs).
//
// Inputs  : SettingValueProviderManager, SettingDefinitionManager
// Outputs : string? (valeur résolue) ou IReadOnlyList<SettingValue>
// =============================================================================

using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Providers;
using DigitalDynamics.Foundation.Settings.Values;

namespace DigitalDynamics.Foundation.Settings.Services;

/// <summary>
/// Implémentation de <see cref="ISettingProvider"/> avec résolution en cascade.
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

            // Sans héritage, on ne descend pas vers les niveaux de moindre priorité
            if (!definition.IsInherited)
            {
                return null;
            }
        }

        return null;
    }
}
