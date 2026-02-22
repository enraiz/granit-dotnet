// =============================================================================
// ConfigurationSettingValueProvider - Provider IConfiguration (niveau C)
// =============================================================================
// Lit IConfiguration["Settings:{name}"] pour surcharger les valeurs par défaut
// depuis appsettings.json ou les variables d'environnement.
// Précédence : User > Tenant > Global > Configuration > Default
//
// Inputs  : IConfiguration, section "Settings:{settingName}"
// Outputs : SettingValue(Name, "C", null, configValue) ou null si absent
// =============================================================================

using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Values;
using Microsoft.Extensions.Configuration;

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Provider de paramètres depuis <see cref="IConfiguration"/>.
/// Lit la section <c>Settings:{name}</c> (order = 400, avant Default).
/// </summary>
public sealed class ConfigurationSettingValueProvider : ISettingValueProvider
{
    /// <summary>Identifiant du provider Configuration.</summary>
    public const string ProviderName = "C";

    private readonly IConfiguration _configuration;

    public ConfigurationSettingValueProvider(IConfiguration configuration) =>
        _configuration = configuration;

    /// <inheritdoc/>
    public string Name => ProviderName;

    /// <inheritdoc/>
    public int Order => 400;

    /// <inheritdoc/>
    public Task<SettingValue?> GetOrNullAsync(SettingDefinition definition, CancellationToken ct = default)
    {
        string? value = _configuration[$"Settings:{definition.Name}"];
        SettingValue? result = value is not null
            ? new SettingValue(definition.Name, ProviderName, null, value)
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
