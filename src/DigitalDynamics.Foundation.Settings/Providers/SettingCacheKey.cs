// =============================================================================
// SettingCacheKey - Cache key convention for setting values
// =============================================================================
// Format  : "{providerName}:{providerKey}:{settingName}"
// Example : "G:global:MyApp.Theme", "T:abc123:MyApp.Theme"
// Shared between providers and SettingManager for key consistency.
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Builds cache keys for setting values.
/// </summary>
internal static class SettingCacheKey
{
    internal static string Build(string providerName, string? providerKey, string name) =>
        $"{providerName}:{providerKey ?? "global"}:{name}";
}
