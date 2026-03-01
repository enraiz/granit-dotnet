namespace Granit.ReferenceData.Endpoints.Dtos;

/// <summary>
/// Paginated response for reference data list endpoints.
/// </summary>
/// <typeparam name="TEntity">The concrete reference data entity type.</typeparam>
/// <param name="Items">The matching entries for the current page.</param>
/// <param name="TotalCount">The total number of matching entries (before pagination).</param>
public sealed record ReferenceDataListResponse<TEntity>(
    IReadOnlyList<TEntity> Items,
    int TotalCount) where TEntity : ReferenceDataEntity;
