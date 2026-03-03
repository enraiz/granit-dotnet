using Granit.DataExchange.Import.Mapping;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Tests.Mapping;

public sealed class MappingResultTests
{
    [Fact]
    public void Succeeded_is_true_when_entity_and_no_errors()
    {
        // Arrange
        MappingResult<TestEntity> result = new()
        {
            Entity = new TestEntity { Name = "Test" },
            Errors = [],
        };

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Succeeded_is_false_when_entity_is_null()
    {
        // Arrange
        MappingResult<TestEntity> result = new()
        {
            Entity = null,
            Errors = [],
        };

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public void Succeeded_is_false_when_errors_present()
    {
        // Arrange
        MappingResult<TestEntity> result = new()
        {
            Entity = new TestEntity { Name = "Test" },
            Errors =
            [
                new CellConversionError("Col1", "Name", "bad", "String", "Granit:DataExchange:InvalidFormat"),
            ],
        };

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public void Default_errors_is_empty_list()
    {
        // Arrange
        MappingResult<TestEntity> result = new() { Entity = new TestEntity() };

        // Assert
        result.Errors.ShouldBeEmpty();
    }
}
