// ---------------------------------------------------------------------------
// LocalizationResourceStore.cs
// Collection typée pour enregistrer et récupérer les ressources de
// localisation. Utilisé dans FoundationLocalizationOptions.
// ---------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace DigitalDynamics.Foundation.Localization;

/// <summary>
/// Dictionnaire de ressources de localisation enregistrées, indexé par type marker.
/// </summary>
public sealed class LocalizationResourceStore
{
    private readonly Dictionary<Type, LocalizationResourceInfo> _resources = [];

    /// <summary>
    /// Enregistre une nouvelle ressource de localisation.
    /// Si le type est déjà enregistré, l'entrée est remplacée.
    /// </summary>
    /// <typeparam name="TResource">Type marker de la ressource.</typeparam>
    /// <param name="defaultCulture">Culture par défaut (défaut : "fr").</param>
    /// <returns>Info de la ressource pour chaînage fluent.</returns>
    public LocalizationResourceInfo Add<TResource>(string defaultCulture = "fr")
        => Add(typeof(TResource), defaultCulture);

    /// <summary>
    /// Enregistre une nouvelle ressource de localisation (surcharge non-générique).
    /// Utilisée par l'auto-discovery pour enregistrer dynamiquement des types découverts.
    /// Si le type est déjà enregistré, l'entrée est remplacée.
    /// </summary>
    /// <param name="resourceType">Type marker de la ressource.</param>
    /// <param name="defaultCulture">Culture par défaut (défaut : "fr").</param>
    /// <returns>Info de la ressource pour chaînage fluent.</returns>
    public LocalizationResourceInfo Add(Type resourceType, string defaultCulture = "fr")
    {
        LocalizationResourceInfo info = new(resourceType, defaultCulture);
        _resources[resourceType] = info;
        return info;
    }

    /// <summary>
    /// Récupère la ressource enregistrée pour le type donné.
    /// </summary>
    /// <typeparam name="TResource">Type marker de la ressource.</typeparam>
    /// <returns>Info de la ressource.</returns>
    /// <exception cref="KeyNotFoundException">Si le type n'est pas enregistré.</exception>
    public LocalizationResourceInfo Get<TResource>() =>
        _resources[typeof(TResource)];

    /// <summary>
    /// Tente de récupérer la ressource pour le type donné.
    /// </summary>
    /// <param name="resourceType">Type marker de la ressource.</param>
    /// <param name="info">Info de la ressource si trouvée.</param>
    /// <returns>True si trouvée, false sinon.</returns>
    public bool TryGetValue(Type resourceType, [NotNullWhen(true)] out LocalizationResourceInfo? info) =>
        _resources.TryGetValue(resourceType, out info);

    /// <summary>
    /// Retourne toutes les ressources enregistrées.
    /// </summary>
    public IEnumerable<LocalizationResourceInfo> GetAll() =>
        _resources.Values;
}
