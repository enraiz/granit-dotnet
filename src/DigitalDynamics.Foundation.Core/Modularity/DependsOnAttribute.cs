// =============================================================================
// DependsOnAttribute - Declaration des dependances entre modules
// =============================================================================
// Utilise sur les classes FoundationModule pour declarer les dependances.
// Le systeme de modules resout l'ordre d'initialisation par tri topologique.
//
// Usage :
//   [DependsOn(typeof(FoundationTimingModule))]
//   [DependsOn(typeof(FoundationGuidsModule))]
//   public class FoundationPersistenceModule : FoundationModule { }
// =============================================================================

namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Declare une dependance sur un ou plusieurs modules Foundation.
/// Le systeme garantit que les modules dependants sont charges en premier.
/// Plusieurs attributs <see cref="DependsOnAttribute"/> peuvent etre empiles.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class DependsOnAttribute : Attribute
{
    /// <summary>Types des modules dont ce module depend.</summary>
    public Type[] DependedTypes { get; }

    public DependsOnAttribute(params Type[] dependedTypes)
    {
        DependedTypes = dependedTypes;
    }
}
