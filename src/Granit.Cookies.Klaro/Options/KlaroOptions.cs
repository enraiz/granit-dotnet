using System.ComponentModel.DataAnnotations;

namespace Granit.Cookies.Klaro.Options;

/// <summary>
/// Configuration options for the Klaro CMP integration.
/// Bound to the <c>Klaro</c> configuration section.
/// </summary>
public sealed class KlaroOptions
{
    /// <summary>Section key in the configuration.</summary>
    public const string SectionName = "Klaro";

    /// <summary>
    /// Name of the cookie where Klaro stores consent decisions.
    /// Default: <c>"klaro"</c>.
    /// </summary>
    public string CookieName { get; set; } = "klaro";

    /// <summary>
    /// Maps Klaro service names to RGPD cookie categories.
    /// Each key is a Klaro service name (as declared in the Klaro front-end config),
    /// each value is the <see cref="CookieCategory"/> it belongs to.
    /// </summary>
    /// <example>
    /// <code>
    /// "Klaro": {
    ///   "ServiceMappings": {
    ///     "google-analytics": "Analytics",
    ///     "matomo": "Analytics",
    ///     "youtube": "Marketing",
    ///     "theme-preference": "Preferences"
    ///   }
    /// }
    /// </code>
    /// </example>
    [Required]
    public Dictionary<string, CookieCategory> ServiceMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
