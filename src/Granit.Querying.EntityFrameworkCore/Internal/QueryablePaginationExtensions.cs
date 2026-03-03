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
        CancellationToken ct)
    {
        int totalCount = await source.CountAsync(ct).ConfigureAwait(false);
        int skip = (page - 1) * pageSize;

        List<T> items = await source
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<T>(items, totalCount);
    }

    /// <summary>
    /// Applies keyset/cursor pagination and returns a <see cref="PagedResult{T}"/>.
    /// </summary>
    public static async Task<PagedResult<T>> ApplyCursorPaginationAsync<T>(
        this IQueryable<T> source,
        string? cursor,
        int pageSize,
        string cursorPropertyName,
        CancellationToken ct)
        where T : class
    {
        PropertyInfo? property = typeof(T).GetProperty(
            cursorPropertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property is null)
        {
            List<T> fallback = await source.Take(pageSize).ToListAsync(ct).ConfigureAwait(false);
            return new PagedResult<T>(fallback, fallback.Count);
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
            .ToListAsync(ct)
            .ConfigureAwait(false);

        string? nextCursor = null;
        if (items.Count > pageSize)
        {
            items.RemoveAt(items.Count - 1);
            T lastItem = items[^1];
            object? lastValue = property.GetValue(lastItem);
            if (lastValue is not null)
            {
                nextCursor = CursorEncoder.Encode(lastValue.ToString()!);
            }
        }

        return new PagedResult<T>(items, items.Count, nextCursor);
    }
}
