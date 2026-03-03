using Granit.DataExchange.Import.Reporting;

namespace Granit.DataExchange.Import.Execution;

/// <summary>
/// Persists validated entities to the database in batches.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IImportExecutor<TEntity> where TEntity : class
{
    /// <summary>
    /// Executes the import by persisting the validated entities.
    /// </summary>
    /// <param name="entities">The validated rows to persist (consumed as a stream).</param>
    /// <param name="options">Execution options (batch size, dry-run, error behavior).</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The import report with statistics and row-level errors.</returns>
    Task<ImportReport> ExecuteAsync(
        IAsyncEnumerable<ValidatedRow<TEntity>> entities,
        ImportExecutionOptions options,
        IProgress<ImportProgress>? progress = null,
        CancellationToken ct = default);
}
