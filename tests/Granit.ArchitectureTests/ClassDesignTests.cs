using Granit.ArchitectureTests.Abstractions.Rules;
using Xunit;

namespace Granit.ArchitectureTests;

/// <summary>
/// Validates class design conventions: sealed DbContexts, internal Ef*Store implementations,
/// no MVC controllers, sealed Options classes.
/// </summary>
public sealed class ClassDesignTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = GranitArchitecture.Instance;

    [Fact]
    public void DbContext_classes_should_be_sealed() =>
        ClassDesignRules.DbContextClassesShouldBeSealed(Architecture);

    [Fact]
    public void EfStore_implementations_should_not_be_public() =>
        ClassDesignRules.EfStoreImplementationsShouldNotBePublic(Architecture);

    [Fact]
    public void No_MVC_controllers_allowed() =>
        ClassDesignRules.NoMvcControllersAllowed(Architecture, "Granit.");

    [Fact]
    public void Options_classes_should_be_sealed() =>
        ClassDesignRules.OptionsClassesShouldBeSealed(Architecture, "Granit.");
}
