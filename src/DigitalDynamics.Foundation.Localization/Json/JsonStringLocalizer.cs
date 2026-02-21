// ---------------------------------------------------------------------------
// JsonStringLocalizer.cs
// Implémente IStringLocalizer avec résolution depuis des dictionnaires JSON.
// Supporte : culture fallback natif (CultureInfo.Parent), héritage de
// ressources parentes, paramétrage {0}, et cache thread-safe via Lazy<T>.
// ---------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace DigitalDynamics.Foundation.Localization.Json;

/// <summary>
/// Localizer basé sur des dictionnaires JSON embarqués avec culture fallback et héritage.
/// </summary>
internal sealed class JsonStringLocalizer : IStringLocalizer
{
    private readonly ConcurrentDictionary<string, Lazy<Dictionary<string, string>>> _cultureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<EmbeddedJsonSource> _sources;
    private readonly string _defaultCulture;
    private readonly List<IStringLocalizer> _baseLocalizers;

    /// <summary>
    /// Crée un nouveau localizer JSON.
    /// </summary>
    /// <param name="sources">Sources JSON embarquées pour cette ressource.</param>
    /// <param name="defaultCulture">Culture par défaut de la ressource.</param>
    /// <param name="baseLocalizers">Localizers des ressources parentes (héritage).</param>
    public JsonStringLocalizer(
        List<EmbeddedJsonSource> sources,
        string defaultCulture,
        List<IStringLocalizer> baseLocalizers)
    {
        _sources = sources;
        _defaultCulture = defaultCulture;
        _baseLocalizers = baseLocalizers;
    }

    /// <inheritdoc />
    public LocalizedString this[string name]
    {
        get
        {
            string? value = GetTranslation(name, CultureInfo.CurrentUICulture);
            return value is not null
                ? new LocalizedString(name, value, resourceNotFound: false)
                : new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            string? value = GetTranslation(name, CultureInfo.CurrentUICulture);

            if (value is null)
            {
                return new LocalizedString(name, name, resourceNotFound: true);
            }

            string formatted = arguments.Length == 0
                ? value
                : string.Format(CultureInfo.CurrentCulture, value, arguments);

            return new LocalizedString(name, formatted, resourceNotFound: false);
        }
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        HashSet<string> seen = [];

        // Parcourir la chaîne de cultures
        CultureInfo currentCulture = CultureInfo.CurrentUICulture;
        while (true)
        {
            foreach (KeyValuePair<string, string> kvp in GetOrLoadDictionary(currentCulture.Name).Where(kvp => seen.Add(kvp.Key)))
            {
                yield return new LocalizedString(kvp.Key, kvp.Value, resourceNotFound: false);
            }

            if (!includeParentCultures || currentCulture == CultureInfo.InvariantCulture)
            {
                break;
            }

            currentCulture = currentCulture.Parent;
        }

        // Fallback sur la culture par défaut
        if (includeParentCultures)
        {
            foreach (KeyValuePair<string, string> kvp in GetOrLoadDictionary(_defaultCulture).Where(kvp => seen.Add(kvp.Key)))
            {
                yield return new LocalizedString(kvp.Key, kvp.Value, resourceNotFound: false);
            }
        }

        // Héritage : inclure les clés des parents
        foreach (IStringLocalizer baseLocalizer in _baseLocalizers)
        {
            foreach (LocalizedString localizedString in baseLocalizer.GetAllStrings(includeParentCultures).Where(s => seen.Add(s.Name)))
            {
                yield return localizedString;
            }
        }
    }

    /// <summary>
    /// Résout une traduction en remontant la chaîne de cultures,
    /// puis en cherchant dans les ressources parentes.
    /// </summary>
    private string? GetTranslation(string name, CultureInfo culture)
    {
        // 1. Remonter la chaîne de cultures via CultureInfo.Parent
        CultureInfo currentCulture = culture;
        while (currentCulture != CultureInfo.InvariantCulture)
        {
            Dictionary<string, string> dictionary = GetOrLoadDictionary(currentCulture.Name);
            if (dictionary.TryGetValue(name, out string? value))
            {
                return value;
            }

            currentCulture = currentCulture.Parent;
        }

        // 2. Fallback sur la culture par défaut de la ressource
        Dictionary<string, string> defaultDictionary = GetOrLoadDictionary(_defaultCulture);
        if (defaultDictionary.TryGetValue(name, out string? defaultValue))
        {
            return defaultValue;
        }

        // 3. Héritage : chercher dans les ressources parentes
        foreach (IStringLocalizer baseLocalizer in _baseLocalizers)
        {
            LocalizedString result = baseLocalizer[name];
            if (!result.ResourceNotFound)
            {
                return result.Value;
            }
        }

        // 4. Clé non trouvée
        return null;
    }

    /// <summary>
    /// Charge ou récupère depuis le cache le dictionnaire pour une culture donnée.
    /// Utilise Lazy&lt;T&gt; pour garantir un seul chargement même sous forte concurrence.
    /// </summary>
    private Dictionary<string, string> GetOrLoadDictionary(string cultureName)
    {
        Lazy<Dictionary<string, string>> lazy = _cultureCache.GetOrAdd(
            cultureName,
            name => new Lazy<Dictionary<string, string>>(() => LoadDictionary(name)));

        return lazy.Value;
    }

    /// <summary>
    /// Charge les traductions pour une culture depuis toutes les sources JSON.
    /// Les sources ajoutées en dernier ont priorité (override applicatif).
    /// </summary>
    private Dictionary<string, string> LoadDictionary(string cultureName)
    {
        Dictionary<string, string> merged = new(StringComparer.Ordinal);

        foreach (EmbeddedJsonSource source in _sources)
        {
            Dictionary<string, Dictionary<string, string>> allCultures =
                JsonLocalizationDictionaryBuilder.Build(source.Assembly, source.ResourcePrefix);

            if (allCultures.TryGetValue(cultureName, out Dictionary<string, string>? texts))
            {
                foreach (KeyValuePair<string, string> kvp in texts)
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }
        }

        return merged;
    }
}
