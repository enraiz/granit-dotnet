using DigitalDynamics.Foundation.Settings.Definitions;
using DigitalDynamics.Foundation.Settings.Values;

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Provides the value of a setting for a given level in the resolution cascade.
/// </summary>
public interface ISettingValueProvider
{
    /// <summary>Short provider identifier (e.g. "G", "T", "U", "C", "D").</summary>
    string Name { get; }

    /// <summary>Resolution order (ascending priority: 100 = highest priority).</summary>
    int Order { get; }

    /// <summary>
    /// Returns the setting value for the current context, or <c>null</c> if absent.
    /// </summary>
    Task<SettingValue?> GetOrNullAsync(SettingDefinition definition, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates the setting value for the current context.
    /// </summary>
    Task SetAsync(SettingDefinition definition, string? value, CancellationToken ct = default);

    /// <summary>
    /// Deletes the setting value for the current context.
    /// </summary>
    Task ClearAsync(SettingDefinition definition, CancellationToken ct = default);
}
