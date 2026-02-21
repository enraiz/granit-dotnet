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
        where TModule : FoundationModule
    {
        return LoadModules(typeof(TModule));
    }

    /// <summary>
    /// Charge tous les modules accessibles depuis <paramref name="startupModuleType"/>
    /// et les retourne en ordre topologique (dependances d'abord).
    /// </summary>
    public static IReadOnlyList<ModuleDescriptor> LoadModules(Type startupModuleType)
    {
        var descriptors = new Dictionary<Type, ModuleDescriptor>();
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

        var instance = (FoundationModule)Activator.CreateInstance(moduleType)!;

        var dependencies = moduleType
            .GetCustomAttributes(typeof(DependsOnAttribute), true)
            .Cast<DependsOnAttribute>()
            .SelectMany(a => a.DependedTypes)
            .Distinct()
            .ToArray();

        descriptors[moduleType] = new ModuleDescriptor(moduleType, instance, dependencies);

        foreach (var dep in dependencies)
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
        var inDegree = descriptors.ToDictionary(kv => kv.Key, _ => 0);
        var adjacency = descriptors.ToDictionary(kv => kv.Key, _ => new List<Type>());

        foreach (var (type, descriptor) in descriptors)
        {
            foreach (var dep in descriptor.Dependencies)
            {
                // dep → type : dep doit etre charge avant type
                adjacency[dep].Add(type);
                inDegree[type]++;
            }
        }

        // File des noeuds sans dependance entrante
        var queue = new Queue<Type>(
            inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = new List<ModuleDescriptor>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(descriptors[current]);

            foreach (var neighbor in adjacency[current])
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
            var cycleTypes = descriptors.Keys
                .Except(sorted.Select(d => d.ModuleType))
                .Select(t => t.Name);
            throw new InvalidOperationException(
                $"Dependance circulaire detectee entre les modules : {string.Join(", ", cycleTypes)}.");
        }

        return sorted;
    }
}
