// =============================================================================
// SettingCacheKey - Convention de clé de cache pour les valeurs de paramètres
// =============================================================================
// Format : "{providerName}:{providerKey}:{settingName}"
// Exemple : "G:global:MyApp.Theme", "T:abc123:MyApp.Theme"
// Partagé entre les providers et SettingManager pour la cohérence des clés.
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Construit les clés de cache pour les valeurs de paramètres.
/// </summary>
internal static class SettingCacheKey
{
    internal static string Build(string providerName, string? providerKey, string name) =>
        $"{providerName}:{providerKey ?? "global"}:{name}";
}
