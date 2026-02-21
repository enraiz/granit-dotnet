// ---------------------------------------------------------------------------
// FoundationLocalizationOptions.cs
// Options de configuration du système de localisation Foundation.
// Permet d'enregistrer des ressources, définir la ressource par défaut
// et lister les langues disponibles.
// ---------------------------------------------------------------------------

namespace DigitalDynamics.Foundation.Localization;

/// <summary>
/// Options de configuration du système de localisation Foundation.
/// </summary>
public sealed class FoundationLocalizationOptions
{
    /// <summary>
    /// Dictionnaire des ressources de localisation enregistrées.
    /// </summary>
    public LocalizationResourceStore Resources { get; } = new();

    /// <summary>
    /// Type de ressource par défaut (fallback quand aucune IStringLocalizer&lt;T&gt; spécifique).
    /// </summary>
    public Type? DefaultResourceType { get; set; }

    /// <summary>
    /// Langues disponibles dans l'application (pour UI de sélection de langue).
    /// </summary>
    public List<LanguageInfo> Languages { get; } = [];

    /// <summary>
    /// Active l'auto-discovery des ressources JSON par convention de nommage.
    /// Quand activé, les assemblies chargées sont scannées pour détecter les types
    /// marqués avec <see cref="Attributes.LocalizationResourceNameAttribute"/> et
    /// leurs fichiers JSON embarqués, sans enregistrement explicite <c>AddJson()</c>.
    /// </summary>
    public bool EnableAutoDiscovery { get; set; }
}
