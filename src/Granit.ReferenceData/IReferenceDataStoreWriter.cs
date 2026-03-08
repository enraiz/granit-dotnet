namespace Granit.ReferenceData;

/// <summary>
/// Write-side contract for managing reference data entries.
/// </summary>
/// <typeparam name="TEntity">The concrete reference data entity type.</typeparam>
public interface IReferenceDataStoreWriter<TEntity> where TEntity : ReferenceDataEntity
{
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
