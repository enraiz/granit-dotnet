// ---------------------------------------------------------------------------
// GranitLocalizationOptions.cs
// Configuration options for the Granit localization system.
// Allows registering resources, defining the default resource,
// and listing available languages.
// ---------------------------------------------------------------------------

using System.Globalization;

namespace Granit.Localization.Options;

/// <summary>
/// Configuration options for the Granit localization system.
/// </summary>
public sealed class GranitLocalizationOptions
{
    /// <summary>
    /// Dictionary of registered localization resources.
    /// </summary>
    public LocalizationResourceStore Resources { get; } = new();

    /// <summary>
    /// Default resource type (fallback when no specific IStringLocalizer&lt;T&gt; is available).
    /// </summary>
    public Type? DefaultResourceType { get; set; }

    /// <summary>
    /// Languages available in the application (for a language selection UI).
    /// Also used as <c>SupportedUICultures</c> by <c>UseGranitRequestLocalization</c>.
    /// </summary>
    public List<LanguageInfo> Languages { get; } = [];

    /// <summary>
    /// Optional list of formatting cultures (<c>SupportedCultures</c>).
    /// When empty (default), <see cref="Languages"/> is used for both
    /// <c>SupportedCultures</c> and <c>SupportedUICultures</c>.
    /// When populated, these cultures control number/date/currency formatting
    /// independently from <see cref="Languages"/> (which controls translations).
    /// </summary>
    /// <example>
    /// <code>
    /// // Financial app: fixed en-US formatting, multilingual UI
    /// options.Languages.Add(new LanguageInfo("fr", "Français", "fr", isDefault: true));
    /// options.Languages.Add(new LanguageInfo("en", "English", "us"));
    /// options.FormattingCultures.Add(new CultureInfo("en-US"));
    /// </code>
    /// </example>
    public List<CultureInfo> FormattingCultures { get; } = [];

    /// <summary>
    /// Enables auto-discovery of JSON resources by naming convention.
    /// When enabled, loaded assemblies are scanned to detect types marked with
    /// <see cref="LocalizationResourceNameAttribute"/> and their
    /// embedded JSON files, without an explicit <c>AddJson()</c> registration.
    /// </summary>
    public bool EnableAutoDiscovery { get; set; }
}
