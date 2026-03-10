using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Granit.ArchitectureTests;

/// <summary>
/// Validates class design conventions: sealed DbContexts, internal Ef*Store implementations,
/// no MVC controllers, sealed Options classes.
/// </summary>
public sealed class ClassDesignTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = GranitArchitecture.Instance;

    [Fact]
    public void DbContext_classes_should_be_sealed()
    {
        IArchRule rule = Classes()
            .That().AreAssignableTo(typeof(DbContext))
            .And().AreNot(typeof(DbContext))
            .Should().BeSealed()
            .Because("isolated DbContext pattern requires sealed DbContexts (CLAUDE.md)");

        rule.Check(Architecture);
    }

    [Fact]
    public void EfStore_implementations_should_not_be_public()
    {
        IArchRule rule = Classes()
            .That().HaveNameStartingWith("Ef")
            .And().HaveNameEndingWith("Store")
            .Should().NotBePublic()
            .Because("EF Core store implementations are internal infrastructure details");

        rule.Check(Architecture);
    }

    [Fact]
    public void No_MVC_controllers_allowed()
    {
        // ControllerBase is not in the architecture graph (ASP.NET Core types are not loaded),
        // so we check by dependency target name instead.
        IEnumerable<Class> controllers = Architecture.Classes
            .Where(c => c.FullName.StartsWith("Granit.", StringComparison.Ordinal)
                && c.Dependencies.Any(d =>
                    d.Target.FullName == "Microsoft.AspNetCore.Mvc.ControllerBase"
                    || d.Target.FullName == "Microsoft.AspNetCore.Mvc.Controller"));

        controllers.ShouldBeEmpty(
            "Granit uses Minimal API only — no MVC controllers (architecture.md). " +
            $"Violators: {string.Join(", ", controllers.Select(c => c.FullName))}");
    }

    [Fact]
    public void Options_classes_should_be_sealed()
    {
        // Options classes must be sealed, unless they are base classes inherited by others.
        IEnumerable<Class> optionsClasses = Architecture.Classes
            .Where(c => c.Name.EndsWith("Options", StringComparison.Ordinal)
                && c.FullName.StartsWith("Granit.", StringComparison.Ordinal)
                && c.IsAbstract != true);

        // Collect names of classes that are inherited by other Options classes
        HashSet<string> baseOptionClasses = Architecture.Classes
            .Where(c => c.Name.EndsWith("Options", StringComparison.Ordinal))
            .SelectMany(c => c.Dependencies
                .Where(d => d.Target.Name.EndsWith("Options", StringComparison.Ordinal))
                .Select(d => d.Target.FullName))
            .ToHashSet(StringComparer.Ordinal);

        IEnumerable<Class> unsealed = optionsClasses
            .Where(c => c.IsSealed != true && !baseOptionClasses.Contains(c.FullName));

        unsealed.ShouldBeEmpty(
            "Options classes must be sealed (style-et-nommage.md). " +
            $"Violators: {string.Join(", ", unsealed.Select(c => c.FullName))}");
    }
}
