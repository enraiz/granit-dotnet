// =============================================================================
// SettingsOptions - Options de configuration du module Settings
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Options;

/// <summary>
/// Options de configuration du module de paramètres.
/// </summary>
public sealed class SettingsOptions
{
    /// <summary>Nom de la section de configuration.</summary>
    public const string SectionName = "Settings";

    /// <summary>Durée de vie des entrées en cache (défaut : 30 minutes).</summary>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(30);
}
