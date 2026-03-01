namespace Granit.ReferenceData;

/// <summary>
/// Paginated result for reference data queries.
/// </summary>
/// <typeparam name="TEntity">The concrete reference data entity type.</typeparam>
/// <param name="Items">The matching entries for the current page.</param>
/// <param name="TotalCount">The total number of matching entries (before pagination).</param>
public sealed record ReferenceDataResult<TEntity>(
    IReadOnlyList<TEntity> Items,
    int TotalCount) where TEntity : ReferenceDataEntity;
