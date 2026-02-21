// =============================================================================
// ModuleDescriptor - Descripteur interne d'un module
// =============================================================================
// Associe un type de module a son instance et ses dependances declarees.
// Utilise par ModuleLoader pour le tri topologique.
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Descripteur interne associant un type de module a son instance
/// et ses dependances declarees via <see cref="DependsOnAttribute"/>.
/// </summary>
internal sealed class ModuleDescriptor
{
    public Type ModuleType { get; }
    public FoundationModule Instance { get; }
    public Type[] Dependencies { get; }

    public ModuleDescriptor(Type moduleType, FoundationModule instance, Type[] dependencies)
    {
        ModuleType = moduleType;
        Instance = instance;
        Dependencies = dependencies;
    }
}
