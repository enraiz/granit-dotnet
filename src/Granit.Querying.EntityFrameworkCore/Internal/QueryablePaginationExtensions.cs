using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Granit.Querying.EntityFrameworkCore.Internal;

/// <summary>
/// Extension methods for applying pagination to an <see cref="IQueryable{T}"/>.
/// </summary>
internal static class QueryablePaginationExtensions
{
    /// <summary>
    /// Applies offset pagination (page/pageSize) and returns a <see cref="PagedResult{T}"/>.
    /// </summary>
    public static async Task<PagedResult<T>> ApplyOffsetPaginationAsync<T>(
        this IQueryable<T> source,
        int page,
        int pageSize,
        bool skipTotalCount,
        CancellationToken cancellationToken)
    {
        int skip = (page - 1) * pageSize;

        if (skipTotalCount)
        {
            // Fetch pageSize + 1 to determine HasMore without COUNT(*)
            List<T> items = await source
                .Skip(skip)
                .Take(pageSize + 1)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            bool hasMore = items.Count > pageSize;
            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }

            return new PagedResult<T>(items, TotalCount: null, HasMore: hasMore);
        }

        int totalCount = await source.CountAsync(cancellationToken).ConfigureAwait(false);

        List<T> pagedItems = await source
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        bool hasMorePages = skip + pagedItems.Count < totalCount;
        return new PagedResult<T>(pagedItems, totalCount, HasMore: hasMorePages);
    }

    /// <summary>
    /// Applies keyset/cursor pagination and returns a <see cref="PagedResult{T}"/>.
    /// </summary>
    public static async Task<PagedResult<T>> ApplyCursorPaginationAsync<T>(
        this IQueryable<T> source,
        string? cursor,
        int pageSize,
        string cursorPropertyName,
        CancellationToken cancellationToken)
        where T : class
    {
        PropertyInfo? property = typeof(T).GetProperty(
            cursorPropertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property is null)
        {
            List<T> fallback = await source.Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
            return new PagedResult<T>(fallback, TotalCount: null, HasMore: false);
        }

        IQueryable<T> query = source;

        if (!string.IsNullOrEmpty(cursor))
        {
            // Decode cursor and filter WHERE property > cursorValue
            string? cursorValue = CursorEncoder.Decode<string>(cursor);
            if (cursorValue is not null)
            {
                object? converted = FilterExpressionBuilder.ConvertValue(
                    cursorValue,
                    Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);

                if (converted is not null)
                {
                    ParameterExpression parameter = Expression.Parameter(typeof(T), "e");
                    MemberExpression member = Expression.Property(parameter, property);
                    ConstantExpression constant = Expression.Constant(converted, property.PropertyType);
                    BinaryExpression greaterThan = Expression.GreaterThan(member, constant);
                    var predicate =
                        Expression.Lambda<Func<T, bool>>(greaterThan, parameter);
                    query = query.Where(predicate);
                }
            }
        }

        // Take pageSize + 1 to determine if there are more pages
        List<T> items = await query
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        string? nextCursor = null;
        bool hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
            T lastItem = items[^1];
            object? lastValue = property.GetValue(lastItem);
            if (lastValue is not null)
            {
                nextCursor = CursorEncoder.Encode(lastValue.ToString()!);
            }
        }

        return new PagedResult<T>(items, TotalCount: null, HasMore: hasMore, NextCursor: nextCursor);
    }
}
