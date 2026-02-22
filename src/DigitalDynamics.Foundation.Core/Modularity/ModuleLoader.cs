// =============================================================================
// ModuleLoader - Module discovery and topological sort
// =============================================================================
// Starting from a root TModule, recursively discovers all dependent
// modules via [DependsOn] and sorts them in topological order
// (Kahn's algorithm). Detects circular dependencies.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Discovers, instantiates, and topologically sorts Foundation modules
/// starting from a root module.
/// </summary>
internal static class ModuleLoader
{
    /// <summary>
    /// Loads all modules reachable from <typeparamref name="TModule"/>
    /// and returns them in topological order (dependencies first).
    /// </summary>
    /// <exception cref="InvalidOperationException">Circular dependency detected.</exception>
    public static IReadOnlyList<ModuleDescriptor> LoadModules<TModule>()
        where TModule : FoundationModule
    {
        return LoadModules(typeof(TModule));
    }

    /// <summary>
    /// Loads all modules reachable from <paramref name="startupModuleType"/>
    /// and returns them in topological order (dependencies first).
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
                $"Type '{moduleType.FullName}' does not inherit from FoundationModule.");
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
    /// Topological sort using Kahn's algorithm.
    /// Returns modules in order: dependencies first, root module last.
    /// </summary>
    private static List<ModuleDescriptor> TopologicalSort(
        Dictionary<Type, ModuleDescriptor> descriptors)
    {
        // Compute the in-degree of each node
        var inDegree = descriptors.ToDictionary(kv => kv.Key, _ => 0);
        var adjacency = descriptors.ToDictionary(kv => kv.Key, _ => new List<Type>());

        foreach (var (type, descriptor) in descriptors)
        {
            foreach (var dep in descriptor.Dependencies)
            {
                // dep -> type: dep must be loaded before type
                adjacency[dep].Add(type);
                inDegree[type]++;
            }
        }

        // Queue of nodes with no incoming edges
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
                $"Circular dependency detected among modules: {string.Join(", ", cycleTypes)}.");
        }

        return sorted;
    }
}
