namespace Granit.Querying.Meta;

/// <summary>
/// Complete metadata for a query endpoint, returned by <c>GET /meta</c>.
/// Enables frontend auto-configuration of list views (columns, filters, sorting, pagination).
/// </summary>
public sealed record QueryMetadata
{
    /// <summary>Column definitions for the data table.</summary>
    public required IReadOnlyList<ColumnDefinitionDto> Columns { get; init; }

    /// <summary>Filterable fields with their available operators.</summary>
    public required IReadOnlyList<FilterableFieldDto> FilterableFields { get; init; }

    /// <summary>Sortable field names.</summary>
    public required IReadOnlyList<SortableFieldDto> SortableFields { get; init; }

    /// <summary>Preset filter groups (Odoo-style).</summary>
    public required IReadOnlyList<FilterGroupMetaDto> PresetFilterGroups { get; init; }

    /// <summary>Independent toggleable filters (Odoo-style quick filters).</summary>
    public required IReadOnlyList<QuickFilterMetaDto> QuickFilters { get; init; }

    /// <summary>Date filter shortcuts.</summary>
    public required IReadOnlyList<DateFilterMetaDto> DateFilters { get; init; }

    /// <summary>Fields allowed for group-by operations.</summary>
    public required IReadOnlyList<GroupByFieldDto> GroupByFields { get; init; }

    /// <summary>Pagination configuration.</summary>
    public required PaginationMetaDto Pagination { get; init; }

    /// <summary>Default sort specification, or <c>null</c>.</summary>
    public string? DefaultSort { get; init; }
}
