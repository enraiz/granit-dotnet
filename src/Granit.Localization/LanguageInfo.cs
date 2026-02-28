// ---------------------------------------------------------------------------
// LanguageInfo.cs
// Represents a language available in the application.
// Used for the language selection UI.
// ---------------------------------------------------------------------------

namespace Granit.Localization;

/// <summary>
/// Represents a language available in the application.
/// </summary>
/// <param name="cultureName">Culture code (e.g. "fr", "en", "fr-CA").</param>
/// <param name="displayName">Display name (e.g. "Français", "English").</param>
/// <param name="flagIcon">Optional flag icon (e.g. "fr", "gb").</param>
/// <param name="isDefault">Whether this is the default language for the application.</param>
public sealed class LanguageInfo(
    string cultureName,
    string displayName,
    string? flagIcon = null,
    bool isDefault = false)
{
    /// <summary>
    /// Culture code (e.g. "fr", "en", "fr-CA").
    /// </summary>
    public string CultureName { get; } = cultureName;

    /// <summary>
    /// Display name (e.g. "Français", "English").
    /// </summary>
    public string DisplayName { get; } = displayName;

    /// <summary>
    /// Optional flag icon.
    /// </summary>
    public string? FlagIcon { get; } = flagIcon;

    /// <summary>
    /// Whether this is the default language for the application.
    /// Used by the language selector UI to pre-select the initial language.
    /// </summary>
    public bool IsDefault { get; } = isDefault;
}
