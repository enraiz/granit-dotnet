// =============================================================================
// ConfigurationSettingValueProvider - IConfiguration provider (level C)
// =============================================================================
// Reads IConfiguration["Settings:{name}"] to override default values
// from appsettings.json or environment variables.
// Precedence: User > Tenant > Global > Configuration > Default
//
// Inputs  : IConfiguration, section "Settings:{settingName}"
// Outputs : SettingValue(Name, "C", null, configValue) or null if absent
// =============================================================================

using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Values;
using Microsoft.Extensions.Configuration;

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Settings provider from <see cref="IConfiguration"/>.
/// Reads the <c>Settings:{name}</c> section (order = 400, before Default).
/// </summary>
public sealed class ConfigurationSettingValueProvider : ISettingValueProvider
{
    /// <summary>Configuration provider identifier.</summary>
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
