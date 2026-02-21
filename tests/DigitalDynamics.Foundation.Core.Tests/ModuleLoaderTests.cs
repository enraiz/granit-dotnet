// =============================================================================
// Tests - ModuleLoader
// =============================================================================
// Verifie que le ModuleLoader :
//   - Charge un module unique sans dependance
//   - Respecte l'ordre topologique (chaine lineaire, diamant)
//   - Detecte et rejette les dependances circulaires
//   - Rejette les types qui ne sont pas des FoundationModule
//   - Deduplique les dependances declarees en double
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.Core.Tests;

public sealed class ModuleLoaderTests
{
    // --- Modules de test ---

    public sealed class StandaloneModule : FoundationModule;

    [DependsOn(typeof(StandaloneModule))]
    public sealed class DependentModule : FoundationModule;

    // Chaine lineaire : C → B → A (A est standalone)
    public sealed class ModuleA : FoundationModule;

    [DependsOn(typeof(ModuleA))]
    public sealed class ModuleB : FoundationModule;

    [DependsOn(typeof(ModuleB))]
    public sealed class ModuleC : FoundationModule;

    // Diamant : Root → Left + Right, Left → Shared, Right → Shared
    public sealed class SharedModule : FoundationModule;

    [DependsOn(typeof(SharedModule))]
    public sealed class LeftModule : FoundationModule;

    [DependsOn(typeof(SharedModule))]
    public sealed class RightModule : FoundationModule;

    [DependsOn(typeof(LeftModule), typeof(RightModule))]
    public sealed class DiamondRootModule : FoundationModule;

    // Dependance circulaire
    [DependsOn(typeof(CircularB))]
    public sealed class CircularA : FoundationModule;

    [DependsOn(typeof(CircularA))]
    public sealed class CircularB : FoundationModule;

    // Dependance dupliquee
    [DependsOn(typeof(StandaloneModule))]
    [DependsOn(typeof(StandaloneModule))]
    public sealed class DuplicateDepsModule : FoundationModule;

    // Pas un module
    public sealed class NotAModule;

    // --- Tests ---

    [Fact]
    public void LoadModules_SingleModule_ReturnsOnlyThatModule()
    {
        IReadOnlyList<ModuleDescriptor> modules = ModuleLoader.LoadModules<StandaloneModule>();

        modules.Should().HaveCount(1);
        modules[0].ModuleType.Should().Be<StandaloneModule>();
        modules[0].Instance.Should().BeOfType<StandaloneModule>();
    }

    [Fact]
    public void LoadModules_LinearChain_ReturnsDependenciesFirst()
    {
        // C → B → A : ordre attendu A, B, C
        IReadOnlyList<ModuleDescriptor> modules = ModuleLoader.LoadModules<ModuleC>();

        modules.Should().HaveCount(3);
        List<Type> types = modules.Select(m => m.ModuleType).ToList();
        types.IndexOf(typeof(ModuleA)).Should().BeLessThan(types.IndexOf(typeof(ModuleB)));
        types.IndexOf(typeof(ModuleB)).Should().BeLessThan(types.IndexOf(typeof(ModuleC)));
    }

    [Fact]
    public void LoadModules_Diamond_SharedLoadedOnce()
    {
        // DiamondRoot → Left + Right → Shared
        IReadOnlyList<ModuleDescriptor> modules = ModuleLoader.LoadModules<DiamondRootModule>();

        modules.Should().HaveCount(4);

        // Shared doit apparaitre exactement une fois
        modules.Where(m => m.ModuleType == typeof(SharedModule)).Should().HaveCount(1);

        // Shared doit etre avant Left et Right
        List<Type> types = modules.Select(m => m.ModuleType).ToList();
        types.IndexOf(typeof(SharedModule)).Should().BeLessThan(types.IndexOf(typeof(LeftModule)));
        types.IndexOf(typeof(SharedModule)).Should().BeLessThan(types.IndexOf(typeof(RightModule)));

        // Left et Right doivent etre avant DiamondRoot
        types.IndexOf(typeof(LeftModule)).Should().BeLessThan(types.IndexOf(typeof(DiamondRootModule)));
        types.IndexOf(typeof(RightModule)).Should().BeLessThan(types.IndexOf(typeof(DiamondRootModule)));
    }

    [Fact]
    public void LoadModules_CircularDependency_ThrowsInvalidOperationException()
    {
        Action act = () => ModuleLoader.LoadModules<CircularA>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*circulaire*");
    }

    [Fact]
    public void LoadModules_NonModuleType_ThrowsInvalidOperationException()
    {
        Action act = () => ModuleLoader.LoadModules(typeof(NotAModule));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FoundationModule*");
    }

    [Fact]
    public void LoadModules_DuplicateDependency_DeduplicatesCorrectly()
    {
        IReadOnlyList<ModuleDescriptor> modules = ModuleLoader.LoadModules<DuplicateDepsModule>();

        modules.Should().HaveCount(2);
        modules.Where(m => m.ModuleType == typeof(StandaloneModule)).Should().HaveCount(1);
    }

    [Fact]
    public void LoadModules_WithDependsOn_SetsCorrectDependencies()
    {
        IReadOnlyList<ModuleDescriptor> modules = ModuleLoader.LoadModules<DependentModule>();

        ModuleDescriptor dependent = modules.Single(m => m.ModuleType == typeof(DependentModule));
        dependent.Dependencies.Should().Contain(typeof(StandaloneModule));
    }
}
