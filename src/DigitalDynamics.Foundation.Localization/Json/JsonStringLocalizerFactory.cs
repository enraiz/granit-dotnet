// ---------------------------------------------------------------------------
// JsonStringLocalizerFactory.cs
// Implémente IStringLocalizerFactory pour créer des JsonStringLocalizer.
// Cache thread-safe via ConcurrentDictionary<Type, Lazy<IStringLocalizer>>.
// Résout l'héritage de ressources et les sources JSON enregistrées.
// ---------------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace DigitalDynamics.Foundation.Localization.Json;

/// <summary>
/// Factory de localizers JSON basée sur les ressources enregistrées dans
/// <see cref="FoundationLocalizationOptions"/>.
/// </summary>
internal sealed class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly ConcurrentDictionary<Type, Lazy<IStringLocalizer>> _cache = new();
    private readonly IOptions<FoundationLocalizationOptions> _options;

    public JsonStringLocalizerFactory(IOptions<FoundationLocalizationOptions> options)
    {
        _options = options;

        if (options.Value.EnableAutoDiscovery)
        {
            LocalizationAutoDiscovery.Discover(options.Value);
        }
    }

    /// <inheritdoc />
    public IStringLocalizer Create(Type resourceSource)
    {
        Lazy<IStringLocalizer> lazy = _cache.GetOrAdd(
            resourceSource,
            type => new Lazy<IStringLocalizer>(() => CreateLocalizer(type)));

        return lazy.Value;
    }

    /// <inheritdoc />
    public IStringLocalizer Create(string baseName, string location)
    {
        // Tenter de résoudre le type depuis le nom complet
        Type? resourceType = Type.GetType($"{baseName}, {location}");
        if (resourceType is not null)
        {
            return Create(resourceType);
        }

        // Fallback : créer un localizer vide (clé = valeur retournée)
        return new JsonStringLocalizer([], "fr", []);
    }

    /// <summary>
    /// Crée un localizer pour le type de ressource donné, avec héritage.
    /// </summary>
    private JsonStringLocalizer CreateLocalizer(Type resourceType)
    {
        FoundationLocalizationOptions options = _options.Value;

        if (!options.Resources.TryGetValue(resourceType, out LocalizationResourceInfo? info))
        {
            // Type non enregistré : retourner un localizer vide
            return new JsonStringLocalizer([], "fr", []);
        }

        // Construire les localizers des ressources parentes (récursif)
        List<IStringLocalizer> baseLocalizers = [];
        foreach (Type baseType in info.BaseTypes)
        {
            baseLocalizers.Add(Create(baseType));
        }

        // Vérifier les [InheritResource] sur la classe marker
        Attributes.InheritResourceAttribute[] inheritAttributes =
            (Attributes.InheritResourceAttribute[])resourceType
                .GetCustomAttributes(typeof(Attributes.InheritResourceAttribute), true);

        foreach (Attributes.InheritResourceAttribute attr in inheritAttributes)
        {
            foreach (Type baseResourceType in attr.BaseResourceTypes)
            {
                // Éviter les doublons si déjà déclaré via AddBaseTypes()
                if (!info.BaseTypes.Contains(baseResourceType))
                {
                    baseLocalizers.Add(Create(baseResourceType));
                }
            }
        }

        return new JsonStringLocalizer(info.JsonSources, info.DefaultCulture, baseLocalizers);
    }
}
