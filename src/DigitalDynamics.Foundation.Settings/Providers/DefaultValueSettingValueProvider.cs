// =============================================================================
// DefaultValueSettingValueProvider - Default value provider (level D)
// =============================================================================
// Returns SettingDefinition.DefaultValue. Last link in the cascade.
// No store, no cache — the value is static (declared at compile time).
//
// Input  : SettingDefinition.DefaultValue
// Output : SettingValue(Name, "D", null, DefaultValue) or null if DefaultValue is null
// =============================================================================

using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Values;

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Default value provider: returns <see cref="SettingDefinition.DefaultValue"/>.
/// Last level in the cascade (order = 500).
/// </summary>
public sealed class DefaultValueSettingValueProvider : ISettingValueProvider
{
    /// <summary>Default provider identifier.</summary>
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
