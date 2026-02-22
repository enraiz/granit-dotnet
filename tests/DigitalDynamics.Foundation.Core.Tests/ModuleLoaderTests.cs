// =============================================================================
// Tests - ModuleLoader
// =============================================================================
// Verifies that ModuleLoader:
//   - Loads a single module without dependencies
//   - Respects topological order (linear chain, diamond)
//   - Detects and rejects circular dependencies
//   - Rejects types that are not FoundationModule
//   - Deduplicates duplicate declared dependencies
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using FluentAssertions;
using Xunit;

namespace DigitalDynamics.Foundation.Core.Tests;

public sealed class ModuleLoaderTests
{
    // --- Test modules ---

    public sealed class StandaloneModule : FoundationModule;

    [DependsOn(typeof(StandaloneModule))]
    public sealed class DependentModule : FoundationModule;

    // Linear chain: C → B → A (A is standalone)
    public sealed class ModuleA : FoundationModule;

    [DependsOn(typeof(ModuleA))]
    public sealed class ModuleB : FoundationModule;

    [DependsOn(typeof(ModuleB))]
    public sealed class ModuleC : FoundationModule;

    // Diamond: Root → Left + Right, Left → Shared, Right → Shared
    public sealed class SharedModule : FoundationModule;

    [DependsOn(typeof(SharedModule))]
    public sealed class LeftModule : FoundationModule;

    [DependsOn(typeof(SharedModule))]
    public sealed class RightModule : FoundationModule;

    [DependsOn(typeof(LeftModule), typeof(RightModule))]
    public sealed class DiamondRootModule : FoundationModule;

    // Circular dependency
    [DependsOn(typeof(CircularB))]
    public sealed class CircularA : FoundationModule;

    [DependsOn(typeof(CircularA))]
    public sealed class CircularB : FoundationModule;

    // Duplicate dependency
    [DependsOn(typeof(StandaloneModule))]
    [DependsOn(typeof(StandaloneModule))]
    public sealed class DuplicateDepsModule : FoundationModule;

    // Not a module
    public sealed class NotAModule;

    // --- Tests ---

    [Fact]
    public void LoadModules_SingleModule_ReturnsOnlyThatModule()
    {
        var modules = ModuleLoader.LoadModules<StandaloneModule>();

        modules.Should().HaveCount(1);
        modules[0].ModuleType.Should().Be<StandaloneModule>();
        modules[0].Instance.Should().BeOfType<StandaloneModule>();
    }

    [Fact]
    public void LoadModules_LinearChain_ReturnsDependenciesFirst()
    {
        // C → B → A : expected order A, B, C
        var modules = ModuleLoader.LoadModules<ModuleC>();

        modules.Should().HaveCount(3);
        var types = modules.Select(m => m.ModuleType).ToList();
        types.IndexOf(typeof(ModuleA)).Should().BeLessThan(types.IndexOf(typeof(ModuleB)));
        types.IndexOf(typeof(ModuleB)).Should().BeLessThan(types.IndexOf(typeof(ModuleC)));
    }

    [Fact]
    public void LoadModules_Diamond_SharedLoadedOnce()
    {
        // DiamondRoot → Left + Right → Shared

        var modules = ModuleLoader.LoadModules<DiamondRootModule>();

        modules.Should().HaveCount(4);

        // Shared must appear exactly once
        modules.Where(m => m.ModuleType == typeof(SharedModule)).Should().HaveCount(1);

        // Shared must come before Left and Right
        var types = modules.Select(m => m.ModuleType).ToList();
        types.IndexOf(typeof(SharedModule)).Should().BeLessThan(types.IndexOf(typeof(LeftModule)));
        types.IndexOf(typeof(SharedModule)).Should().BeLessThan(types.IndexOf(typeof(RightModule)));

        // Left and Right must come before DiamondRoot
        types.IndexOf(typeof(LeftModule)).Should().BeLessThan(types.IndexOf(typeof(DiamondRootModule)));
        types.IndexOf(typeof(RightModule)).Should().BeLessThan(types.IndexOf(typeof(DiamondRootModule)));
    }

    [Fact]
    public void LoadModules_CircularDependency_ThrowsInvalidOperationException()
    {
        var act = () => ModuleLoader.LoadModules<CircularA>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Circular dependency*");
    }

    [Fact]
    public void LoadModules_NonModuleType_ThrowsInvalidOperationException()
    {
        var act = () => ModuleLoader.LoadModules(typeof(NotAModule));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FoundationModule*");
    }

    [Fact]
    public void LoadModules_DuplicateDependency_DeduplicatesCorrectly()
    {
        var modules = ModuleLoader.LoadModules<DuplicateDepsModule>();

        modules.Should().HaveCount(2);
        modules.Where(m => m.ModuleType == typeof(StandaloneModule)).Should().HaveCount(1);
    }

    [Fact]
    public void LoadModules_WithDependsOn_SetsCorrectDependencies()
    {
        var modules = ModuleLoader.LoadModules<DependentModule>();

        var dependent = modules.Single(m => m.ModuleType == typeof(DependentModule));
        dependent.Dependencies.Should().Contain(typeof(StandaloneModule));
    }
}
