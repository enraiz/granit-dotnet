// =============================================================================
// DefaultValueSettingValueProvider - Provider de valeur par défaut (niveau D)
// =============================================================================
// Retourne SettingDefinition.DefaultValue. Dernier maillon de la cascade.
// Pas de store, pas de cache — la valeur est statique (déclarée à la compilation).
//
// Input  : SettingDefinition.DefaultValue
// Output : SettingValue(Name, "D", null, DefaultValue) ou null si DefaultValue null
// =============================================================================

using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Values;

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Provider de valeur par défaut : retourne <see cref="SettingDefinition.DefaultValue"/>.
/// Dernier niveau de la cascade (order = 500).
/// </summary>
public sealed class DefaultValueSettingValueProvider : ISettingValueProvider
{
    /// <summary>Identifiant du provider Default.</summary>
    public const string ProviderName = "D";

    /// <inheritdoc/>
    public string Name => ProviderName;

    /// <inheritdoc/>
    public int Order => 500;

    /// <inheritdoc/>
    public Task<SettingValue?> GetOrNullAsync(SettingDefinition definition, CancellationToken ct = default)
    {
        SettingValue? result = definition.DefaultValue is not null
            ? new SettingValue(definition.Name, ProviderName, null, definition.DefaultValue)
            : null;
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task SetAsync(SettingDefinition definition, string? value, CancellationToken ct = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task ClearAsync(SettingDefinition definition, CancellationToken ct = default) =>
        Task.CompletedTask;
}
