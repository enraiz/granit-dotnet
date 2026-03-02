using Granit.Core.Modularity;
using Granit.DataImport.Excel;
using Shouldly;
using Xunit;

namespace Granit.DataImport.Excel.Tests;

public sealed class GranitDataImportExcelModuleTests
{
    [Fact]
    public void Module_has_expected_dependency()
    {
        // Arrange
        DependsOnAttribute[] attributes = typeof(GranitDataImportExcelModule)
            .GetCustomAttributes(typeof(DependsOnAttribute), true)
            .Cast<DependsOnAttribute>()
            .ToArray();

        // Assert
        Type[] dependedTypes = attributes.SelectMany(a => a.DependedTypes).ToArray();
        dependedTypes.ShouldContain(typeof(GranitDataImportModule));
    }

    [Fact]
    public void Module_is_sealed() =>
        typeof(GranitDataImportExcelModule).IsSealed.ShouldBeTrue();

    [Fact]
    public void Module_inherits_from_GranitModule() =>
        typeof(GranitDataImportExcelModule).BaseType.ShouldBe(typeof(GranitModule));
}
