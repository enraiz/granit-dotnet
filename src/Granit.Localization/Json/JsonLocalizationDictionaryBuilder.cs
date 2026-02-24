// ---------------------------------------------------------------------------
// JsonLocalizationDictionaryBuilder.cs
// Reads localization JSON files embedded in an assembly and builds
// culture → (key → value) dictionaries.
// Expected format: { "culture": "fr", "texts": { "Key": "Value" } }
// Supports nested keys flattened with "." (collision detection).
// ---------------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;

namespace Granit.Localization.Json;

/// <summary>
/// Builds localization dictionaries from embedded JSON files.
/// </summary>
internal static class JsonLocalizationDictionaryBuilder
{
    /// <summary>
    /// Reads all embedded JSON files matching the given prefix
    /// and returns a culture → (key → value) dictionary.
    /// </summary>
    /// <param name="assembly">Assembly containing the embedded resources.</param>
    /// <param name="resourcePrefix">Prefix of the embedded resource names.</param>
    /// <returns>A culture → (key → value) dictionary.</returns>
    public static Dictionary<string, Dictionary<string, string>> Build(Assembly assembly, string resourcePrefix)
    {
        Dictionary<string, Dictionary<string, string>> result = new(StringComparer.OrdinalIgnoreCase);
        string[] resourceNames = assembly.GetManifestResourceNames();

        foreach (string resourceName in resourceNames)
        {
            if (!resourceName.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase) ||
                !resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            (string culture, Dictionary<string, string> texts) = ParseJsonStream(stream, resourceName);

            if (result.TryGetValue(culture, out Dictionary<string, string>? existing))
            {
                foreach (KeyValuePair<string, string> kvp in texts)
                {
                    existing[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                result[culture] = texts;
            }
        }

        return result;
    }

    /// <summary>
    /// Parses a JSON stream and returns the culture and texts.
    /// </summary>
    private static (string Culture, Dictionary<string, string> Texts) ParseJsonStream(
        Stream stream, string resourceName)
    {
        using var document = JsonDocument.Parse(stream);
        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("culture", out JsonElement cultureElement))
        {
            throw new InvalidOperationException(
                $"The JSON file '{resourceName}' does not contain a 'culture' property.");
        }

        string culture = cultureElement.GetString()
            ?? throw new InvalidOperationException(
                $"The 'culture' property is null in '{resourceName}'.");

        Dictionary<string, string> texts = new(StringComparer.Ordinal);

        if (root.TryGetProperty("texts", out JsonElement textsElement))
        {
            FlattenJsonElement(textsElement, "", texts, resourceName);
        }

        return (culture, texts);
    }

    /// <summary>
    /// Recursively flattens a JSON element into keys using "." as separator.
    /// Detects collisions between flat keys and nested keys.
    /// </summary>
    private static void FlattenJsonElement(
        JsonElement element,
        string prefix,
        Dictionary<string, string> result,
        string resourceName)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            string key = string.IsNullOrEmpty(prefix)
                ? property.Name
                : $"{prefix}.{property.Name}";

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                FlattenJsonElement(property.Value, key, result, resourceName);
            }
            else
            {
                string value = property.Value.GetString()
                    ?? throw new InvalidOperationException(
                        $"The value of key '{key}' is null in '{resourceName}'.");

                if (!result.TryAdd(key, value))
                {
                    throw new InvalidOperationException(
                        $"Key collision detected in '{resourceName}': " +
                        $"key '{key}' already exists (conflict between flat key and nested key).");
                }
            }
        }
    }
}
