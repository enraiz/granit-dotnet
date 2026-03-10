using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Granit.ArchitectureTests.Abstractions.Rules;

/// <summary>
/// Reusable class design rules: sealed DbContexts, internal EfStores, no MVC, sealed Options.
/// </summary>
public static class ClassDesignRules
{
    /// <summary>
    /// All DbContext subclasses must be sealed (isolated DbContext pattern).
    /// </summary>
    public static void DbContextClassesShouldBeSealed(ArchUnitNET.Domain.Architecture architecture)
    {
        IArchRule rule = Classes()
            .That().AreAssignableTo(typeof(DbContext))
            .And().AreNot(typeof(DbContext))
            .Should().BeSealed()
            .Because("isolated DbContext pattern requires sealed DbContexts");

        rule.Check(architecture);
    }

    /// <summary>
    /// Ef*Store implementations must not be public (internal infrastructure detail).
    /// </summary>
    public static void EfStoreImplementationsShouldNotBePublic(ArchUnitNET.Domain.Architecture architecture)
    {
        IArchRule rule = Classes()
            .That().HaveNameStartingWith("Ef")
            .And().HaveNameEndingWith("Store")
            .Should().NotBePublic()
            .Because("EF Core store implementations are internal infrastructure details");

        rule.Check(architecture);
    }

    /// <summary>
    /// No MVC controllers allowed — Minimal API only.
    /// </summary>
    public static void NoMvcControllersAllowed(ArchUnitNET.Domain.Architecture architecture, string typePrefix)
    {
        IEnumerable<Class> controllers = architecture.Classes
            .Where(c => c.FullName.StartsWith(typePrefix, StringComparison.Ordinal)
                && c.Dependencies.Any(d =>
                    d.Target.FullName == "Microsoft.AspNetCore.Mvc.ControllerBase"
                    || d.Target.FullName == "Microsoft.AspNetCore.Mvc.Controller"));

        controllers.ShouldBeEmpty(
            "Minimal API only — no MVC controllers allowed. " +
            $"Violators: {string.Join(", ", controllers.Select(c => c.FullName))}");
    }

    /// <summary>
    /// Options classes must be sealed (unless they serve as base classes for other Options).
    /// </summary>
    public static void OptionsClassesShouldBeSealed(ArchUnitNET.Domain.Architecture architecture, string typePrefix)
    {
        IEnumerable<Class> optionsClasses = architecture.Classes
            .Where(c => c.Name.EndsWith("Options", StringComparison.Ordinal)
                && c.FullName.StartsWith(typePrefix, StringComparison.Ordinal)
                && c.IsAbstract != true);

        // Collect names of classes that are inherited by other Options classes
        HashSet<string> baseOptionClasses = architecture.Classes
            .Where(c => c.Name.EndsWith("Options", StringComparison.Ordinal))
            .SelectMany(c => c.Dependencies
                .Where(d => d.Target.Name.EndsWith("Options", StringComparison.Ordinal))
                .Select(d => d.Target.FullName))
            .ToHashSet(StringComparer.Ordinal);

        IEnumerable<Class> unsealed = optionsClasses
            .Where(c => c.IsSealed != true && !baseOptionClasses.Contains(c.FullName));

        unsealed.ShouldBeEmpty(
            "Options classes must be sealed. " +
            $"Violators: {string.Join(", ", unsealed.Select(c => c.FullName))}");
    }
}
