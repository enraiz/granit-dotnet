using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Shouldly;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Granit.ArchitectureTests;

/// <summary>
/// Validates layered architecture dependency rules:
/// Core → Application → Infrastructure → Endpoints (no reverse).
/// See docs/patterns/architecture/layered-architecture.md.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = GranitArchitecture.Instance;

    [Fact]
    public void Core_types_should_not_depend_on_EntityFrameworkCore()
    {
        // Granit.Core types live in sub-namespaces (Granit.Core.Domain, Granit.Core.Modularity, etc.)
        IEnumerable<IType> coreTypes = Architecture.Types
            .Where(t => t.FullName.StartsWith("Granit.Core.", StringComparison.Ordinal));

        IEnumerable<IType> efCoreDeps = coreTypes
            .Where(t => t.Dependencies
                .Any(d => d.Target.FullName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)));

        efCoreDeps.ShouldBeEmpty(
            "Core layer must not depend on EF Core infrastructure (layered architecture). " +
            $"Violators: {string.Join(", ", efCoreDeps.Select(t => t.FullName))}");
    }

    [Fact]
    public void Timing_types_should_not_depend_on_EntityFrameworkCore()
    {
        IEnumerable<IType> timingTypes = Architecture.Types
            .Where(t => t.FullName.StartsWith("Granit.Timing", StringComparison.Ordinal));

        IEnumerable<IType> efCoreDeps = timingTypes
            .Where(t => t.Dependencies
                .Any(d => d.Target.FullName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)));

        efCoreDeps.ShouldBeEmpty(
            "Granit.Timing is a core utility — no infrastructure dependencies");
    }

    [Fact]
    public void Guids_types_should_not_depend_on_EntityFrameworkCore()
    {
        IEnumerable<IType> guidsTypes = Architecture.Types
            .Where(t => t.FullName.StartsWith("Granit.Guids", StringComparison.Ordinal));

        IEnumerable<IType> efCoreDeps = guidsTypes
            .Where(t => t.Dependencies
                .Any(d => d.Target.FullName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)));

        efCoreDeps.ShouldBeEmpty(
            "Granit.Guids is a core utility — no infrastructure dependencies");
    }

    [Fact]
    public void Endpoint_types_should_not_depend_on_EntityFrameworkCore()
    {
        // All types in *.Endpoints namespaces
        IEnumerable<IType> endpointTypes = Architecture.Types
            .Where(t => t.Namespace.FullName.EndsWith(".Endpoints", StringComparison.Ordinal) ||
                        t.Namespace.FullName.Contains(".Endpoints.", StringComparison.Ordinal));

        IEnumerable<IType> efCoreDeps = endpointTypes
            .Where(t => t.Dependencies
                .Any(d => d.Target.FullName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)));

        efCoreDeps.ShouldBeEmpty(
            "Endpoints must use abstractions (ports), not EF Core directly. " +
            $"Violators: {string.Join(", ", efCoreDeps.Select(t => t.FullName))}");
    }

    [Fact]
    public void IQueryable_should_not_appear_in_non_persistence_types()
    {
        // IQueryable<T> must not escape the persistence layer — it's a leaky abstraction
        // that exposes EF Core internals. Only *.EntityFrameworkCore, *.Querying, and *.Persistence may use it.
        IEnumerable<IType> violators = Architecture.Types
            .Where(t => !t.Namespace.FullName.Contains("EntityFrameworkCore", StringComparison.Ordinal)
                && !t.Namespace.FullName.Contains("Querying", StringComparison.Ordinal)
                && !t.Namespace.FullName.Contains("Persistence", StringComparison.Ordinal)
                && !t.Namespace.FullName.Contains("DataExchange", StringComparison.Ordinal))
            .Where(t => t.Dependencies
                .Any(d => d.Target.FullName.StartsWith("System.Linq.IQueryable", StringComparison.Ordinal)));

        violators.ShouldBeEmpty(
            "IQueryable<T> must not escape the persistence layer (architecture.md). " +
            $"Violators: {string.Join(", ", violators.Select(t => t.FullName))}");
    }
}
