// =============================================================================
// ModuleLoader - Decouverte et tri topologique des modules
// =============================================================================
// A partir d'un module racine TModule, decouvre recursivement tous les
// modules dependants via [DependsOn] et les trie par ordre topologique
// (algorithme de Kahn). Detecte les dependances circulaires.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Decouvre, instancie et trie topologiquement les modules Foundation
/// a partir d'un module racine.
/// </summary>
internal static class ModuleLoader
{
    /// <summary>
    /// Charge tous les modules accessibles depuis <typeparamref name="TModule"/>
    /// et les retourne en ordre topologique (dependances d'abord).
    /// </summary>
    /// <exception cref="InvalidOperationException">Dependance circulaire detectee.</exception>
    public static IReadOnlyList<ModuleDescriptor> LoadModules<TModule>()
        where TModule : FoundationModule =>
        LoadModules(typeof(TModule));

    /// <summary>
    /// Charge tous les modules accessibles depuis <paramref name="startupModuleType"/>
    /// et les retourne en ordre topologique (dependances d'abord).
    /// </summary>
    public static IReadOnlyList<ModuleDescriptor> LoadModules(Type startupModuleType)
    {
        Dictionary<Type, ModuleDescriptor> descriptors = [];
        DiscoverModules(startupModuleType, descriptors);
        return TopologicalSort(descriptors);
    }

    private static void DiscoverModules(Type moduleType, Dictionary<Type, ModuleDescriptor> descriptors)
    {
        if (descriptors.ContainsKey(moduleType))
        {
            return;
        }

        if (!typeof(FoundationModule).IsAssignableFrom(moduleType))
        {
            throw new InvalidOperationException(
                $"Le type '{moduleType.FullName}' n'herite pas de FoundationModule.");
        }

        FoundationModule instance = (FoundationModule)Activator.CreateInstance(moduleType)!;

        Type[] dependencies = moduleType
            .GetCustomAttributes(typeof(DependsOnAttribute), true)
            .Cast<DependsOnAttribute>()
            .SelectMany(a => a.DependedTypes)
            .Distinct()
            .ToArray();

        descriptors[moduleType] = new ModuleDescriptor(moduleType, instance, dependencies);

        foreach (Type dep in dependencies)
        {
            DiscoverModules(dep, descriptors);
        }
    }

    /// <summary>
    /// Tri topologique par algorithme de Kahn.
    /// Retourne les modules en ordre : dependances d'abord, module racine en dernier.
    /// </summary>
    private static List<ModuleDescriptor> TopologicalSort(
        Dictionary<Type, ModuleDescriptor> descriptors)
    {
        // Calculer le degre entrant de chaque noeud
        Dictionary<Type, int> inDegree = descriptors.ToDictionary(kv => kv.Key, _ => 0);
        Dictionary<Type, List<Type>> adjacency = descriptors.ToDictionary(kv => kv.Key, _ => new List<Type>());

        foreach ((Type type, ModuleDescriptor descriptor) in descriptors)
        {
            foreach (Type dep in descriptor.Dependencies)
            {
                // dep → type : dep doit etre charge avant type
                adjacency[dep].Add(type);
                inDegree[type]++;
            }
        }

        // File des noeuds sans dependance entrante
        Queue<Type> queue = new(
            inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        List<ModuleDescriptor> sorted = [];

        while (queue.Count > 0)
        {
            Type current = queue.Dequeue();
            sorted.Add(descriptors[current]);

            foreach (Type neighbor in adjacency[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (sorted.Count != descriptors.Count)
        {
            IEnumerable<string> cycleTypes = descriptors.Keys
                .Except(sorted.Select(d => d.ModuleType))
                .Select(t => t.Name);
            throw new InvalidOperationException(
                $"Dependance circulaire detectee entre les modules : {string.Join(", ", cycleTypes)}.");
        }

        return sorted;
    }
}
