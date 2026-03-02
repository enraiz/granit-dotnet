using Granit.Core.Modularity;
using Granit.DataImport.Csv;
using Shouldly;
using Xunit;

namespace Granit.DataImport.Csv.Tests;

public sealed class GranitDataImportCsvModuleTests
{
    [Fact]
    public void Module_has_expected_dependency()
    {
        // Arrange
        DependsOnAttribute[] attributes = typeof(GranitDataImportCsvModule)
            .GetCustomAttributes(typeof(DependsOnAttribute), true)
            .Cast<DependsOnAttribute>()
            .ToArray();

        // Assert
        Type[] dependedTypes = attributes.SelectMany(a => a.DependedTypes).ToArray();
        dependedTypes.ShouldContain(typeof(GranitDataImportModule));
    }

    [Fact]
    public void Module_is_sealed() =>
        typeof(GranitDataImportCsvModule).IsSealed.ShouldBeTrue();

    [Fact]
    public void Module_inherits_from_GranitModule() =>
        typeof(GranitDataImportCsvModule).BaseType.ShouldBe(typeof(GranitModule));
}
