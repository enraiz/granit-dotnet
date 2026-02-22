namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Declares a dependency on one or more Foundation modules.
/// The system guarantees that dependent modules are loaded first.
/// Multiple <see cref="DependsOnAttribute"/> attributes may be stacked.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class DependsOnAttribute : Attribute
{
    /// <summary>Types of the modules this module depends on.</summary>
    public Type[] DependedTypes { get; }

    public DependsOnAttribute(params Type[] dependedTypes)
    {
        DependedTypes = dependedTypes;
    }
}
