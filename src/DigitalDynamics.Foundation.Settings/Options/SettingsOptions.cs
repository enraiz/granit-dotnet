// =============================================================================
// SettingsOptions - Configuration options for the Settings module
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Options;

/// <summary>
/// Configuration options for the settings module.
/// </summary>
public sealed class SettingsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Settings";

    /// <summary>Cache entry lifetime (default: 30 minutes).</summary>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(30);
}
