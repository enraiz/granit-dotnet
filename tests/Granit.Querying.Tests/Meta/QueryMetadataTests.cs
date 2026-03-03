using Granit.Querying.Meta;
using Shouldly;
using Xunit;

namespace Granit.Querying.Tests.Meta;

public sealed class QueryMetadataTests
{
    [Fact]
    public void All_required_properties_are_set()
    {
        QueryMetadata metadata = new()
        {
            Columns = [new ColumnDefinitionDto("Name", "Nom", "String", 1, true, true, true, null)],
            FilterableFields = [new FilterableFieldDto("Name", "String", [Granit.Querying.Filtering.FilterOperator.Eq])],
            SortableFields = [new SortableFieldDto("Name")],
            PresetFilterGroups = [new FilterGroupMetaDto("Status", "Statut", [new PresetMetaDto("Active", "Actif", true)])],
            QuickFilters = [new QuickFilterMetaDto("MyItems", "Mes éléments", true)],
            DateFilters = [new DateFilterMetaDto("CreatedAt", DatePeriod.ThisMonth, [DatePeriod.Today, DatePeriod.ThisMonth, DatePeriod.ThisYear])],
            GroupByFields = [new GroupByFieldDto("Status", "String")],
            Pagination = new PaginationMetaDto(20, 100, true),
            DefaultSort = "-createdAt",
        };

        metadata.Columns.Count.ShouldBe(1);
        metadata.FilterableFields.Count.ShouldBe(1);
        metadata.SortableFields.Count.ShouldBe(1);
        metadata.PresetFilterGroups.Count.ShouldBe(1);
        metadata.DateFilters.Count.ShouldBe(1);
        metadata.GroupByFields.Count.ShouldBe(1);
        metadata.Pagination.DefaultPageSize.ShouldBe(20);
        metadata.Pagination.MaxPageSize.ShouldBe(100);
        metadata.Pagination.SupportsCursor.ShouldBeTrue();
        metadata.DefaultSort.ShouldBe("-createdAt");
    }

    [Fact]
    public void ColumnDefinitionDto_properties()
    {
        ColumnDefinitionDto column = new("LastName", "Nom de famille", "String", 2, true, true, true, null);

        column.Name.ShouldBe("LastName");
        column.Label.ShouldBe("Nom de famille");
        column.Type.ShouldBe("String");
        column.Order.ShouldBe(2);
        column.IsSortable.ShouldBeTrue();
        column.IsFilterable.ShouldBeTrue();
        column.IsVisible.ShouldBeTrue();
        column.Format.ShouldBeNull();
    }

    [Fact]
    public void FilterableFieldDto_properties()
    {
        FilterableFieldDto field = new("Age", "Int32",
            [Granit.Querying.Filtering.FilterOperator.Eq, Granit.Querying.Filtering.FilterOperator.Gt, Granit.Querying.Filtering.FilterOperator.Lt]);

        field.Name.ShouldBe("Age");
        field.Type.ShouldBe("Int32");
        field.Operators.Count.ShouldBe(3);
    }

    [Fact]
    public void FilterGroupMetaDto_with_presets()
    {
        FilterGroupMetaDto group = new("Status", "Statut",
            [new PresetMetaDto("Active", "Actif", true), new PresetMetaDto("Archived", "Archivé", false)]);

        group.Name.ShouldBe("Status");
        group.Label.ShouldBe("Statut");
        group.Presets.Count.ShouldBe(2);
        group.Presets[0].IsDefault.ShouldBeTrue();
        group.Presets[1].IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void DateFilterMetaDto_properties()
    {
        DateFilterMetaDto filter = new("CreatedAt", DatePeriod.ThisMonth,
            [DatePeriod.Today, DatePeriod.ThisWeek, DatePeriod.ThisMonth]);

        filter.Name.ShouldBe("CreatedAt");
        filter.DefaultPeriod.ShouldBe(DatePeriod.ThisMonth);
        filter.AvailablePeriods.Count.ShouldBe(3);
    }

    [Fact]
    public void PaginationMetaDto_properties()
    {
        PaginationMetaDto pagination = new(25, 200, false);

        pagination.DefaultPageSize.ShouldBe(25);
        pagination.MaxPageSize.ShouldBe(200);
        pagination.SupportsCursor.ShouldBeFalse();
    }

    [Fact]
    public void QuickFilterMetaDto_properties()
    {
        QuickFilterMetaDto filter = new("MyAppointments", "Mes rendez-vous", true);

        filter.Name.ShouldBe("MyAppointments");
        filter.Label.ShouldBe("Mes rendez-vous");
        filter.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void DefaultSort_can_be_null()
    {
        QueryMetadata metadata = new()
        {
            Columns = [],
            FilterableFields = [],
            SortableFields = [],
            PresetFilterGroups = [],
            QuickFilters = [],
            DateFilters = [],
            GroupByFields = [],
            Pagination = new PaginationMetaDto(20, 100, false),
        };

        metadata.DefaultSort.ShouldBeNull();
    }
}
