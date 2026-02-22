namespace DigitalDynamics.Foundation.Core.Modularity;

/// <summary>
/// Descripteur interne associant un type de module a son instance
/// et ses dependances declarees via <see cref="DependsOnAttribute"/>.
/// </summary>
internal sealed class ModuleDescriptor(Type moduleType, FoundationModule instance, Type[] dependencies)
{
    public Type ModuleType { get; } = moduleType;
    public FoundationModule Instance { get; } = instance;
    public Type[] Dependencies { get; } = dependencies;
}
