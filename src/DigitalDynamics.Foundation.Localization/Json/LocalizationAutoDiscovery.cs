// ---------------------------------------------------------------------------
// LocalizationAutoDiscovery.cs
// Scanne les assemblies chargées pour détecter automatiquement les ressources
// de localisation JSON par convention de nommage.
//
// Convention : {anything}.Localization.{ResourceName}.{culture}.json
// Exemple    : DigitalDynamics.Foundation.Vault.Localization.Vault.fr.json
//
// Déclenché quand FoundationLocalizationOptions.EnableAutoDiscovery = true.
// Les ressources déjà enregistrées explicitement ne sont pas écrasées.
// ---------------------------------------------------------------------------

using System.Reflection;
using DigitalDynamics.Foundation.Localization.Attributes;

namespace DigitalDynamics.Foundation.Localization.Json;

/// <summary>
/// Découverte automatique des ressources de localisation JSON par convention.
/// </summary>
internal static class LocalizationAutoDiscovery
{
    private const string LocalizationSegment = ".Localization.";

    /// <summary>
    /// Scanne toutes les assemblies chargées dans l'AppDomain et enregistre
    /// les ressources de localisation découvertes par convention.
    /// </summary>
    /// <param name="options">Options de localisation à enrichir.</param>
    public static void Discover(FoundationLocalizationOptions options)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly assembly in assemblies)
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            DiscoverFromAssembly(assembly, options);
        }
    }

    private static void DiscoverFromAssembly(Assembly assembly, FoundationLocalizationOptions options)
    {
        string[] resourceNames;
        try
        {
            resourceNames = assembly.GetManifestResourceNames();
        }
        catch (NotSupportedException)
        {
            return;
        }

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.OfType<Type>().ToArray();
        }

        foreach (Type type in types)
        {
            LocalizationResourceNameAttribute? attr =
                type.GetCustomAttribute<LocalizationResourceNameAttribute>();

            if (attr is null)
            {
                continue;
            }

            // Ne pas écraser un enregistrement explicite
            if (options.Resources.TryGetValue(type, out _))
            {
                continue;
            }

            // Rechercher le préfixe JSON : *.Localization.{Name}.{culture}.json
            string pattern = $"{LocalizationSegment}{attr.Name}.";
            string? prefix = FindPrefix(resourceNames, pattern);

            if (prefix is null)
            {
                continue;
            }

            // Détecter les types parents via [InheritResource]
            InheritResourceAttribute[] inheritAttrs =
                (InheritResourceAttribute[])type.GetCustomAttributes(
                    typeof(InheritResourceAttribute), inherit: true);

            Type[] baseTypes = inheritAttrs
                .SelectMany(a => a.BaseResourceTypes)
                .ToArray();

            LocalizationResourceInfo info = options.Resources
                .Add(type, attr.DefaultCulture)
                .AddJson(assembly, prefix);

            if (baseTypes.Length > 0)
            {
                info.AddBaseTypes(baseTypes);
            }
        }
    }

    /// <summary>
    /// Retourne le préfixe de ressource embarquée correspondant au pattern donné,
    /// ou null si aucun fichier JSON ne correspond.
    /// </summary>
    private static string? FindPrefix(string[] resourceNames, string pattern)
    {
        foreach (string resourceName in resourceNames)
        {
            if (!resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int idx = resourceName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                continue;
            }

            // prefix = tout jusqu'à la fin de "{Name}" (sans le "." final du pattern)
            return resourceName[..(idx + pattern.Length - 1)];
        }

        return null;
    }
}
