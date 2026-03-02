using Shouldly;
using Xunit;

namespace Granit.ReferenceData.Tests;

public sealed class ReferenceDataResultTests
{
    private sealed class TestEntity : ReferenceDataEntity;

    [Fact]
    public void Items_And_TotalCount_Are_Preserved()
    {
        TestEntity entity = new() { Code = "BE", LabelEn = "Belgium" };
        List<TestEntity> items = [entity];

        ReferenceDataResult<TestEntity> result = new(items, 42);

        result.Items.ShouldBe(items);
        result.TotalCount.ShouldBe(42);
    }

    [Fact]
    public void Empty_Result_Has_Zero_TotalCount()
    {
        ReferenceDataResult<TestEntity> result = new([], 0);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }
}
