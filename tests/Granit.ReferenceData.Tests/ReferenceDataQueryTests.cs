using Shouldly;
using Xunit;

namespace Granit.ReferenceData.Tests;

public sealed class ReferenceDataQueryTests
{
    [Fact]
    public void Default_ActiveOnly_Is_True()
    {
        ReferenceDataQuery query = new();

        query.ActiveOnly.ShouldBeTrue();
    }

    [Fact]
    public void Default_SearchTerm_Is_Null()
    {
        ReferenceDataQuery query = new();

        query.SearchTerm.ShouldBeNull();
    }

    [Fact]
    public void Default_SortBy_Is_SortOrder()
    {
        ReferenceDataQuery query = new();

        query.SortBy.ShouldBe("SortOrder");
    }

    [Fact]
    public void Default_Descending_Is_False()
    {
        ReferenceDataQuery query = new();

        query.Descending.ShouldBeFalse();
    }

    [Fact]
    public void Default_Skip_Is_Null()
    {
        ReferenceDataQuery query = new();

        query.Skip.ShouldBeNull();
    }

    [Fact]
    public void Default_Take_Is_Null()
    {
        ReferenceDataQuery query = new();

        query.Take.ShouldBeNull();
    }

    [Fact]
    public void Custom_Values_Are_Preserved()
    {
        ReferenceDataQuery query = new(
            ActiveOnly: false,
            SearchTerm: "belg",
            SortBy: "Code",
            Descending: true,
            Skip: 10,
            Take: 25);

        query.ActiveOnly.ShouldBeFalse();
        query.SearchTerm.ShouldBe("belg");
        query.SortBy.ShouldBe("Code");
        query.Descending.ShouldBeTrue();
        query.Skip.ShouldBe(10);
        query.Take.ShouldBe(25);
    }
}
