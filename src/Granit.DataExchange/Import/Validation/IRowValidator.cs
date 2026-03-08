namespace Granit.DataExchange.Import.Validation;

/// <summary>
/// Validates a mapped entity before persistence.
/// Delegates to <c>GranitValidator&lt;TEntity&gt;</c> (FluentValidation, CascadeMode.Continue).
/// </summary>
/// <typeparam name="TEntity">The entity type being validated.</typeparam>
public interface IRowValidator<in TEntity> where TEntity : class
{
    /// <summary>
    /// Validates the entity and returns all errors (CascadeMode.Continue).
    /// </summary>
    /// <param name="entity">The mapped entity to validate.</param>
    /// <param name="rowNumber">The source row number (for error reporting).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validation result containing any field errors.</returns>
    Task<RowValidationResult> ValidateAsync(
        TEntity entity,
        int rowNumber,
        CancellationToken cancellationToken = default);
}
