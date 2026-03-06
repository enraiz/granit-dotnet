using System.Runtime.CompilerServices;
using Granit.Querying.Filtering;
using Granit.Querying.Meta;
using Granit.Querying.SavedViews;
using Microsoft.EntityFrameworkCore;

namespace Granit.Querying.EntityFrameworkCore.Internal;

/// <summary>
/// Default implementation of <see cref="IQueryEngine{TEntity}"/>.
/// Pipeline: parse filters → whitelist validate → apply filters → apply presets
/// → apply global search → apply sort → count → apply pagination (or group by).
/// </summary>
internal sealed class QueryEngine<TEntity>(
    QueryDefinition<TEntity> definition) : IQueryEngine<TEntity>
    where TEntity : class
{
    private readonly QueryDefinitionBuilder<TEntity> _builder = definition.GetBuilder();

    /// <inheritdoc/>
    public async Task<PagedResult<TEntity>> ExecuteAsync(
        IQueryable<TEntity> source,
        QueryRequest request,
        CancellationToken ct = default)
    {
        IQueryable<TEntity> query = ApplyCommonFilters(source, request);

        // Sort
        query = query.ApplySort(request.Sort, _builder);

        // Pagination
        int pageSize = ClampPageSize(request.PageSize);

        if (request.Cursor is not null && _builder.CursorPropertyName is not null)
        {
            return await query.ApplyCursorPaginationAsync(
                request.Cursor, pageSize, _builder.CursorPropertyName, ct)
                .ConfigureAwait(false);
        }

        int page = request.Page ?? 1;
        if (page < 1)
        {
            page = 1;
        }

        return await query.ApplyOffsetPaginationAsync(page, pageSize, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<GroupedResult<TEntity>> ExecuteGroupedAsync(
        IQueryable<TEntity> source,
        QueryRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.GroupBy))
        {
            return new GroupedResult<TEntity>([], 0);
        }

        IQueryable<TEntity> query = ApplyCommonFilters(source, request);

        return await query.ApplyGroupByAsync(request.GroupBy, _builder, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TEntity> ExecuteStreamAsync(
        IQueryable<TEntity> source,
        QueryRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        IQueryable<TEntity> query = ApplyCommonFilters(source, request);
        query = query.ApplySort(request.Sort, _builder);

        await foreach (TEntity entity in query.AsAsyncEnumerable().WithCancellation(ct).ConfigureAwait(false))
        {
            yield return entity;
        }
    }

    /// <inheritdoc/>
    public QueryMetadata GetMetadata(IReadOnlyList<SavedViewSummary>? savedViews = null) =>
        new()
        {
            Columns = _builder.Columns.Select(c => new ColumnDefinition(
                c.PropertyName,
                c.Label ?? c.PropertyName,
                c.ClrType.Name,
                c.Order,
                c.IsSortable,
                c.IsFilterable,
                c.IsVisible,
                c.Format)).ToList(),
            FilterableFields = _builder.Columns
                .Where(c => c.IsFilterable)
                .Select(c => new FilterableField(
                    c.PropertyName,
                    c.ClrType.Name,
                    FilterOperatorInference.GetOperators(c.ClrType)))
                .ToList(),
            SortableFields = _builder.Columns
                .Where(c => c.IsSortable)
                .Select(c => new SortableField(c.PropertyName))
                .ToList(),
            PresetFilterGroups = _builder.FilterGroups.Select(g => new FilterGroupMeta(
                g.Name,
                g.Label ?? g.Name,
                g.Presets.Select(p => new PresetMeta(
                    p.Name,
                    p.Label ?? p.Name,
                    p.IsDefault)).ToList())).ToList(),
            QuickFilters = _builder.QuickFilters.Select(f => new QuickFilterMeta(
                f.Name,
                f.Label ?? f.Name,
                f.IsDefault)).ToList(),
            DateFilters = _builder.DateFilters.Select(d => new DateFilterMeta(
                d.PropertyName,
                d.DefaultPeriod,
                Enum.GetValues<DatePeriod>().ToList())).ToList(),
            GroupByFields = _builder.GroupByFields.Select(g => new GroupByField(
                g.PropertyName,
                g.ClrType.Name)).ToList(),
            Pagination = new PaginationMeta(
                _builder.DefaultPageSizeValue,
                _builder.MaxPageSizeValue,
                _builder.CursorPropertyName is not null),
            DefaultSort = _builder.DefaultSortValue,
        };

    private IQueryable<TEntity> ApplyCommonFilters(IQueryable<TEntity> source, QueryRequest request)
    {
        IQueryable<TEntity> query = source;

        // Parse and apply filters
        if (request.Filter is not null)
        {
            List<FilterCriteria> criteria = ParseFilterCriteria(request.Filter);
            query = query.ApplyFilters(criteria, _builder);
        }

        // Apply presets
        query = query.ApplyPresets(request.Presets, _builder);

        // Apply quick filters
        query = query.ApplyQuickFilters(request.QuickFilters, _builder);

        // Apply global search
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.ApplyGlobalSearch(request.Search, _builder);
        }

        return query;
    }

    private int ClampPageSize(int? requestedSize)
    {
        if (requestedSize is null || requestedSize <= 0)
        {
            return _builder.DefaultPageSizeValue;
        }

        return Math.Min(requestedSize.Value, _builder.MaxPageSizeValue);
    }

    private static List<FilterCriteria> ParseFilterCriteria(IReadOnlyDictionary<string, string> filter)
    {
        List<FilterCriteria> criteria = [];

        foreach ((string key, string value) in filter)
        {
            int dotIndex = key.LastIndexOf('.');
            if (dotIndex <= 0 || dotIndex >= key.Length - 1)
            {
                continue;
            }

            string field = key[..dotIndex];
            string operatorStr = key[(dotIndex + 1)..];

            if (Enum.TryParse<FilterOperator>(operatorStr, ignoreCase: true, out FilterOperator op))
            {
                criteria.Add(new FilterCriteria(field, op, value));
            }
        }

        return criteria;
    }
}
