// ---------------------------------------------------------------------------
// FoundationLocalizationOptions.cs
// Configuration options for the Foundation localization system.
// Allows registering resources, defining the default resource,
// and listing available languages.
// ---------------------------------------------------------------------------

namespace DigitalDynamics.Foundation.Localization;

/// <summary>
/// Configuration options for the Foundation localization system.
/// </summary>
public sealed class FoundationLocalizationOptions
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
    /// </summary>
    public List<LanguageInfo> Languages { get; } = [];

    /// <summary>
    /// Enables auto-discovery of JSON resources by naming convention.
    /// When enabled, loaded assemblies are scanned to detect types marked with
    /// <see cref="Attributes.LocalizationResourceNameAttribute"/> and their
    /// embedded JSON files, without an explicit <c>AddJson()</c> registration.
    /// </summary>
    public bool EnableAutoDiscovery { get; set; }
}
