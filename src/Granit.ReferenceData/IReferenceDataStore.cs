namespace Granit.ReferenceData;

/// <summary>
/// Generic store abstraction for reference data CRUD operations.
/// </summary>
/// <typeparam name="TEntity">The concrete reference data entity type.</typeparam>
public interface IReferenceDataStore<TEntity> where TEntity : ReferenceDataEntity
{
    /// <summary>
    /// Retrieves a filtered, sorted, and paginated list of reference data entries.
    /// </summary>
    /// <param name="query">Optional query parameters. When <c>null</c>, returns all active entries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the matching items and total count.</returns>
    Task<ReferenceDataResult<TEntity>> GetAllAsync(
        ReferenceDataQuery? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single entry by its unique <see cref="ReferenceDataEntity.Code"/>.
    /// </summary>
    /// <param name="code">The business key (e.g., "BE").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching entity, or <c>null</c> if not found.</returns>
    Task<TEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new reference data entry. The <see cref="ReferenceDataEntity.Code"/> must be unique.
    /// </summary>
    /// <param name="entity">The entity to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing reference data entry identified by its <see cref="ReferenceDataEntity.Code"/>.
    /// </summary>
    /// <param name="entity">The entity with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates or deactivates a reference data entry (soft toggle).
    /// </summary>
    /// <param name="code">The business key of the entry.</param>
    /// <param name="isActive">The new active status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetActiveAsync(string code, bool isActive, CancellationToken cancellationToken = default);
}
