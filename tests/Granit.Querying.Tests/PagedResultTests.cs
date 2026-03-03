using Shouldly;
using Xunit;

namespace Granit.Querying.Tests;

public sealed class PagedResultTests
{
    [Fact]
    public void Items_And_TotalCount_Are_Preserved()
    {
        List<string> items = ["Alice", "Bob"];

        PagedResult<string> result = new(items, 42);

        result.Items.ShouldBe(items);
        result.TotalCount.ShouldBe(42);
    }

    [Fact]
    public void Default_NextCursor_Is_Null()
    {
        PagedResult<int> result = new([1, 2, 3], 10);

        result.NextCursor.ShouldBeNull();
    }

    [Fact]
    public void NextCursor_Is_Preserved_When_Provided()
    {
        PagedResult<int> result = new([1, 2, 3], 100, "eyJpZCI6M30=");

        result.NextCursor.ShouldBe("eyJpZCI6M30=");
    }

    [Fact]
    public void Empty_Result_Has_Zero_TotalCount()
    {
        PagedResult<string> result = new([], 0);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public void Record_Equality_Works()
    {
        List<string> items = ["Alice"];

        PagedResult<string> a = new(items, 1);
        PagedResult<string> b = new(items, 1);

        a.ShouldBe(b);
    }
}
