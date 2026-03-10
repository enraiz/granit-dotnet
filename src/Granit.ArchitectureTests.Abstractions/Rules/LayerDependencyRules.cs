using ArchUnitNET.Domain;
using Shouldly;

namespace Granit.ArchitectureTests.Abstractions.Rules;

/// <summary>
/// Reusable layered architecture rules: core/endpoints isolation from EF Core, IQueryable confinement.
/// </summary>
public static class LayerDependencyRules
{
    /// <summary>
    /// Types in the given namespace prefix must not depend on EF Core.
    /// </summary>
    public static void TypesShouldNotDependOnEntityFrameworkCore(
        ArchUnitNET.Domain.Architecture architecture,
        string namespacePrefix,
        string layerDescription)
    {
        IEnumerable<IType> types = architecture.Types
            .Where(t => t.FullName.StartsWith(namespacePrefix, StringComparison.Ordinal));

        IEnumerable<IType> efCoreDeps = types
            .Where(t => t.Dependencies
                .Any(d => d.Target.FullName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)));

        efCoreDeps.ShouldBeEmpty(
            $"{layerDescription} must not depend on EF Core infrastructure. " +
            $"Violators: {string.Join(", ", efCoreDeps.Select(t => t.FullName))}");
    }

    /// <summary>
    /// Endpoint types (namespace containing ".Endpoints") must not depend on EF Core.
    /// </summary>
    public static void EndpointTypesShouldNotDependOnEntityFrameworkCore(ArchUnitNET.Domain.Architecture architecture)
    {
        IEnumerable<IType> endpointTypes = architecture.Types
            .Where(t => t.Namespace.FullName.EndsWith(".Endpoints", StringComparison.Ordinal) ||
                        t.Namespace.FullName.Contains(".Endpoints.", StringComparison.Ordinal));

        IEnumerable<IType> efCoreDeps = endpointTypes
            .Where(t => t.Dependencies
                .Any(d => d.Target.FullName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)));

        efCoreDeps.ShouldBeEmpty(
            "Endpoints must use abstractions (ports), not EF Core directly. " +
            $"Violators: {string.Join(", ", efCoreDeps.Select(t => t.FullName))}");
    }

    /// <summary>
    /// IQueryable must not escape the persistence/data layer.
    /// Types in the given allowed namespaces are exempt.
    /// </summary>
    public static void IQueryableShouldNotEscapePersistenceLayer(
        ArchUnitNET.Domain.Architecture architecture,
        params string[] allowedNamespaceFragments)
    {
        string[] defaultAllowed = ["EntityFrameworkCore", "Querying", "Persistence"];
        string[] allAllowed = [.. defaultAllowed, .. allowedNamespaceFragments];

        IEnumerable<IType> violators = architecture.Types
            .Where(t => !allAllowed.Any(ns =>
                t.Namespace.FullName.Contains(ns, StringComparison.Ordinal)))
            .Where(t => t.Dependencies
                .Any(d => d.Target.FullName.StartsWith("System.Linq.IQueryable", StringComparison.Ordinal)));

        violators.ShouldBeEmpty(
            "IQueryable<T> must not escape the persistence layer. " +
            $"Violators: {string.Join(", ", violators.Select(t => t.FullName))}");
    }
}
