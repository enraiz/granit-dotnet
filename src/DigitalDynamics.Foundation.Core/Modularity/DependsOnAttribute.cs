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
