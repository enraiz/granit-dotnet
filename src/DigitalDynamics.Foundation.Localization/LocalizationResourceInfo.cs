// ---------------------------------------------------------------------------
// LocalizationResourceInfo.cs
// Représente une ressource de localisation enregistrée : type marker,
// culture par défaut, sources JSON embarquées et chaîne d'héritage.
// API fluent pour l'enregistrement (AddJson, AddBaseTypes).
// ---------------------------------------------------------------------------

using System.Reflection;

namespace DigitalDynamics.Foundation.Localization;

/// <summary>
/// Informations d'enregistrement d'une ressource de localisation.
/// </summary>
public sealed class LocalizationResourceInfo
{
    /// <summary>
    /// Type marker de la ressource (classe vide avec attributs).
    /// </summary>
    public Type ResourceType { get; }

    /// <summary>
    /// Culture par défaut pour cette ressource (ex: "fr").
    /// </summary>
    public string DefaultCulture { get; }

    /// <summary>
    /// Types des ressources parentes (héritage de traductions).
    /// </summary>
    public List<Type> BaseTypes { get; } = [];

    /// <summary>
    /// Sources JSON embarquées associées à cette ressource.
    /// </summary>
    internal List<EmbeddedJsonSource> JsonSources { get; } = [];

    /// <summary>
    /// Crée une nouvelle info de ressource de localisation.
    /// </summary>
    /// <param name="resourceType">Type marker de la ressource.</param>
    /// <param name="defaultCulture">Culture par défaut.</param>
    public LocalizationResourceInfo(Type resourceType, string defaultCulture)
    {
        ResourceType = resourceType;
        DefaultCulture = defaultCulture;
    }

    /// <summary>
    /// Ajoute une source de fichiers JSON embarqués.
    /// </summary>
    /// <param name="assembly">Assembly contenant les ressources embarquées.</param>
    /// <param name="embeddedResourcePrefix">Préfixe des noms de ressources (séparateur point).</param>
    /// <returns>Cette instance pour chaînage fluent.</returns>
    public LocalizationResourceInfo AddJson(Assembly assembly, string embeddedResourcePrefix)
    {
        JsonSources.Add(new EmbeddedJsonSource(assembly, embeddedResourcePrefix));
        return this;
    }

    /// <summary>
    /// Ajoute des types de ressources parentes pour l'héritage de traductions.
    /// </summary>
    /// <param name="types">Types des ressources parentes.</param>
    /// <returns>Cette instance pour chaînage fluent.</returns>
    public LocalizationResourceInfo AddBaseTypes(params Type[] types)
    {
        BaseTypes.AddRange(types);
        return this;
    }
}
