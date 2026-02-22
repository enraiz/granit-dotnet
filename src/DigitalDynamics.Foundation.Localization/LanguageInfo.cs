// ---------------------------------------------------------------------------
// LanguageInfo.cs
// Représente une langue disponible dans l'application.
// Utilisé pour l'UI de sélection de langue.
// ---------------------------------------------------------------------------

namespace DigitalDynamics.Foundation.Localization;

/// <summary>
/// Représente une langue disponible dans l'application.
/// </summary>
/// <param name="cultureName">Code culture (ex: "fr", "en", "fr-CA").</param>
/// <param name="displayName">Nom affiché (ex: "Français", "English").</param>
/// <param name="flagIcon">Icône de drapeau optionnelle (ex: "fr", "gb").</param>
public sealed class LanguageInfo(string cultureName, string displayName, string? flagIcon = null)
{
    /// <summary>
    /// Code culture (ex: "fr", "en", "fr-CA").
    /// </summary>
    public string CultureName { get; } = cultureName;

    /// <summary>
    /// Nom affiché (ex: "Français", "English").
    /// </summary>
    public string DisplayName { get; } = displayName;

    /// <summary>
    /// Icône de drapeau optionnelle.
    /// </summary>
    public string? FlagIcon { get; } = flagIcon;
}
