using Granit.Core.Modularity;
using Granit.DataImport.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Granit.DataImport.EntityFrameworkCore.Tests;

public sealed class GranitDataImportEntityFrameworkCoreModuleTests
{
    [Fact]
    public void Module_has_expected_dependency()
    {
        // Arrange
        DependsOnAttribute[] attributes = typeof(GranitDataImportEntityFrameworkCoreModule)
            .GetCustomAttributes(typeof(DependsOnAttribute), true)
            .Cast<DependsOnAttribute>()
            .ToArray();

        // Assert
        Type[] dependedTypes = attributes.SelectMany(a => a.DependedTypes).ToArray();
        dependedTypes.ShouldContain(typeof(GranitDataImportModule));
    }

    [Fact]
    public void Module_is_sealed() =>
        typeof(GranitDataImportEntityFrameworkCoreModule).IsSealed.ShouldBeTrue();

    [Fact]
    public void Module_inherits_from_GranitModule() =>
        typeof(GranitDataImportEntityFrameworkCoreModule).BaseType.ShouldBe(typeof(GranitModule));
}
