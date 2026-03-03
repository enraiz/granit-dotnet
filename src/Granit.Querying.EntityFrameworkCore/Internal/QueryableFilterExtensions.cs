using System.Linq.Expressions;
using Granit.Querying.Filtering;

namespace Granit.Querying.EntityFrameworkCore.Internal;

/// <summary>
/// Extension methods for applying filters, global search, and presets to an <see cref="IQueryable{T}"/>.
/// </summary>
internal static class QueryableFilterExtensions
{
    /// <summary>
    /// Applies parsed filter criteria to the queryable, validating each field against the
    /// whitelist of filterable columns declared in the definition builder.
    /// </summary>
    public static IQueryable<TEntity> ApplyFilters<TEntity>(
        this IQueryable<TEntity> source,
        IReadOnlyList<FilterCriteria> criteria,
        QueryDefinitionBuilder<TEntity> builder)
        where TEntity : class
    {
        var filterableFields = builder.Columns
            .Where(c => c.IsFilterable)
            .Select(c => c.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IQueryable<TEntity> query = source;

        foreach (FilterCriteria criterion in criteria)
        {
            if (!filterableFields.Contains(criterion.Field))
            {
                continue; // Silently ignore non-whitelisted fields
            }

            Expression<Func<TEntity, bool>>? predicate =
                FilterExpressionBuilder.Build<TEntity>(criterion);

            if (predicate is not null)
            {
                query = query.Where(predicate);
            }
        }

        return query;
    }

    /// <summary>
    /// Applies global free-text search across all declared global search properties.
    /// Uses OR semantics (any property matching satisfies the search).
    /// </summary>
    public static IQueryable<TEntity> ApplyGlobalSearch<TEntity>(
        this IQueryable<TEntity> source,
        string searchTerm,
        QueryDefinitionBuilder<TEntity> builder)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || builder.GlobalSearchProperties.Count == 0)
        {
            return source;
        }

        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "e");
        Expression? combined = null;

        foreach (string propertyName in builder.GlobalSearchProperties)
        {
            MemberExpression member = Expression.Property(parameter, propertyName);
            if (member.Type != typeof(string))
            {
                continue;
            }

            // e.Property != null && e.Property.Contains(searchTerm)
            Expression notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
            Expression contains = Expression.Call(
                member,
                typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!,
                Expression.Constant(searchTerm));
            Expression predicate = Expression.AndAlso(notNull, contains);

            combined = combined is null ? predicate : Expression.OrElse(combined, predicate);
        }

        if (combined is null)
        {
            return source;
        }

        return source.Where(Expression.Lambda<Func<TEntity, bool>>(combined, parameter));
    }

    /// <summary>
    /// Applies preset filters. OR within each group, AND between groups.
    /// </summary>
    public static IQueryable<TEntity> ApplyPresets<TEntity>(
        this IQueryable<TEntity> source,
        IReadOnlyDictionary<string, string>? activePresets,
        QueryDefinitionBuilder<TEntity> builder)
        where TEntity : class
    {
        if (activePresets is null || activePresets.Count == 0)
        {
            // Apply default presets
            return ApplyDefaultPresets(source, builder);
        }

        IQueryable<TEntity> query = source;

        foreach (FilterGroupDescriptor group in builder.FilterGroups)
        {
            if (!activePresets.TryGetValue(group.Name, out string? presetNames))
            {
                continue;
            }

            HashSet<string> active = presetNames
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Expression<Func<TEntity, bool>>? groupPredicate = BuildGroupPredicate<TEntity>(group, active);
            if (groupPredicate is not null)
            {
                query = query.Where(groupPredicate);
            }
        }

        return query;
    }

    private static IQueryable<TEntity> ApplyDefaultPresets<TEntity>(
        IQueryable<TEntity> source,
        QueryDefinitionBuilder<TEntity> builder)
        where TEntity : class
    {
        IQueryable<TEntity> query = source;

        foreach (FilterGroupDescriptor group in builder.FilterGroups)
        {
            var defaults = group.Presets
                .Where(p => p.IsDefault)
                .ToList();

            if (defaults.Count == 0)
            {
                continue;
            }

            var defaultNames = defaults.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Expression<Func<TEntity, bool>>? predicate = BuildGroupPredicate<TEntity>(group, defaultNames);
            if (predicate is not null)
            {
                query = query.Where(predicate);
            }
        }

        return query;
    }

    private static Expression<Func<TEntity, bool>>? BuildGroupPredicate<TEntity>(
        FilterGroupDescriptor group,
        HashSet<string> activeNames)
        where TEntity : class
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "e");
        Expression? combined = null;

        foreach (PresetDescriptor preset in group.Presets)
        {
            if (!activeNames.Contains(preset.Name))
            {
                continue;
            }

            // Rebind the preset predicate to use our parameter
            var typedPredicate = (Expression<Func<TEntity, bool>>)preset.Predicate;
            Expression body = new ParameterReplacer(typedPredicate.Parameters[0], parameter)
                .Visit(typedPredicate.Body);

            combined = combined is null ? body : Expression.OrElse(combined, body);
        }

        if (combined is null)
        {
            return null;
        }

        return Expression.Lambda<Func<TEntity, bool>>(combined, parameter);
    }

    /// <summary>
    /// Replaces one parameter expression with another in an expression tree.
    /// </summary>
    private sealed class ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == oldParam ? newParam : base.VisitParameter(node);
    }
}
