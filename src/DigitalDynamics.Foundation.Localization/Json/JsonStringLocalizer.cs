// ---------------------------------------------------------------------------
// JsonStringLocalizer.cs
// Implements IStringLocalizer with resolution from JSON dictionaries.
// Supports: native culture fallback (CultureInfo.Parent), parent resource
// inheritance, {0} formatting, and thread-safe cache via Lazy<T>.
// ---------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace DigitalDynamics.Foundation.Localization.Json;

/// <summary>
/// Localizer based on embedded JSON dictionaries with culture fallback and inheritance.
/// </summary>
internal sealed class JsonStringLocalizer : IStringLocalizer
{
    private readonly ConcurrentDictionary<string, Lazy<Dictionary<string, string>>> _cultureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<EmbeddedJsonSource> _sources;
    private readonly string _defaultCulture;
    private readonly List<IStringLocalizer> _baseLocalizers;

    /// <summary>
    /// Creates a new JSON localizer.
    /// </summary>
    /// <param name="sources">Embedded JSON sources for this resource.</param>
    /// <param name="defaultCulture">Default culture of the resource.</param>
    /// <param name="baseLocalizers">Localizers of parent resources (inheritance).</param>
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

        // Walk the culture chain
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

        // Fallback to the default culture
        if (includeParentCultures)
        {
            foreach (KeyValuePair<string, string> kvp in GetOrLoadDictionary(_defaultCulture).Where(kvp => seen.Add(kvp.Key)))
            {
                yield return new LocalizedString(kvp.Key, kvp.Value, resourceNotFound: false);
            }
        }

        // Inheritance: include keys from parent resources
        foreach (IStringLocalizer baseLocalizer in _baseLocalizers)
        {
            foreach (LocalizedString localizedString in baseLocalizer.GetAllStrings(includeParentCultures).Where(s => seen.Add(s.Name)))
            {
                yield return localizedString;
            }
        }
    }

    /// <summary>
    /// Resolves a translation by walking up the culture chain,
    /// then searching in parent resources.
    /// </summary>
    private string? GetTranslation(string name, CultureInfo culture)
    {
        // 1. Walk up the culture chain via CultureInfo.Parent
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

        // 2. Fallback to the resource's default culture
        Dictionary<string, string> defaultDictionary = GetOrLoadDictionary(_defaultCulture);
        if (defaultDictionary.TryGetValue(name, out string? defaultValue))
        {
            return defaultValue;
        }

        // 3. Inheritance: search in parent resources
        foreach (IStringLocalizer baseLocalizer in _baseLocalizers)
        {
            LocalizedString result = baseLocalizer[name];
            if (!result.ResourceNotFound)
            {
                return result.Value;
            }
        }

        // 4. Key not found
        return null;
    }

    /// <summary>
    /// Loads or retrieves from cache the dictionary for a given culture.
    /// Uses Lazy&lt;T&gt; to guarantee a single load even under high concurrency.
    /// </summary>
    private Dictionary<string, string> GetOrLoadDictionary(string cultureName)
    {
        Lazy<Dictionary<string, string>> lazy = _cultureCache.GetOrAdd(
            cultureName,
            name => new Lazy<Dictionary<string, string>>(() => LoadDictionary(name)));

        return lazy.Value;
    }

    /// <summary>
    /// Loads translations for a culture from all JSON sources.
    /// Sources added last take priority (application-level override).
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
