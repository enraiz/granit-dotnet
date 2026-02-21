// ---------------------------------------------------------------------------
// JsonLocalizationDictionaryBuilder.cs
// Lit les fichiers JSON de localisation embarqués dans une assembly et
// construit des dictionnaires culture → (clé → valeur).
// Format attendu : { "culture": "fr", "texts": { "Key": "Valeur" } }
// Supporte les clés imbriquées aplaties avec "." (détection de collisions).
// ---------------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;

namespace DigitalDynamics.Foundation.Localization.Json;

/// <summary>
/// Construit des dictionnaires de localisation depuis des fichiers JSON embarqués.
/// </summary>
internal static class JsonLocalizationDictionaryBuilder
{
    /// <summary>
    /// Lit tous les fichiers JSON embarqués correspondant au préfixe donné
    /// et retourne un dictionnaire culture → (clé → valeur).
    /// </summary>
    /// <param name="assembly">Assembly contenant les ressources embarquées.</param>
    /// <param name="resourcePrefix">Préfixe des noms de ressources embarquées.</param>
    /// <returns>Dictionnaire culture → (clé → valeur).</returns>
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
    /// Parse un flux JSON et retourne la culture et les textes.
    /// </summary>
    private static (string Culture, Dictionary<string, string> Texts) ParseJsonStream(
        Stream stream, string resourceName)
    {
        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("culture", out JsonElement cultureElement))
        {
            throw new InvalidOperationException(
                $"Le fichier JSON '{resourceName}' ne contient pas de propriété 'culture'.");
        }

        string culture = cultureElement.GetString()
            ?? throw new InvalidOperationException(
                $"La propriété 'culture' est null dans '{resourceName}'.");

        Dictionary<string, string> texts = new(StringComparer.Ordinal);

        if (root.TryGetProperty("texts", out JsonElement textsElement))
        {
            FlattenJsonElement(textsElement, "", texts, resourceName);
        }

        return (culture, texts);
    }

    /// <summary>
    /// Aplatit récursivement un élément JSON en clés avec séparateur ".".
    /// Détecte les collisions entre clés plates et clés imbriquées.
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
                        $"La valeur de la clé '{key}' est null dans '{resourceName}'.");

                if (!result.TryAdd(key, value))
                {
                    throw new InvalidOperationException(
                        $"Collision de clés détectée dans '{resourceName}' : " +
                        $"la clé '{key}' existe déjà (conflit entre clé plate et clé imbriquée).");
                }
            }
        }
    }
}
