using Granit.Core.Modularity;
using Granit.Timing;
using Granit.Validation;
using Shouldly;
using Xunit;

namespace Granit.DataImport.Tests;

public sealed class GranitDataImportModuleTests
{
    [Fact]
    public void Module_has_expected_dependencies()
    {
        // Arrange
        DependsOnAttribute[] attributes = typeof(GranitDataImportModule)
            .GetCustomAttributes(typeof(DependsOnAttribute), true)
            .Cast<DependsOnAttribute>()
            .ToArray();

        // Assert
        Type[] dependedTypes = attributes.SelectMany(a => a.DependedTypes).ToArray();
        dependedTypes.ShouldContain(typeof(GranitTimingModule));
        dependedTypes.ShouldContain(typeof(GranitValidationModule));
    }

    [Fact]
    public void Module_is_sealed()
    {
        typeof(GranitDataImportModule).IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void Module_inherits_from_GranitModule()
    {
        typeof(GranitDataImportModule).BaseType.ShouldBe(typeof(GranitModule));
    }
}
