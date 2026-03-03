using Granit.DataExchange.Import.Reporting;

namespace Granit.DataExchange.Import.Pipeline;

/// <summary>
/// Orchestrates the full import pipeline for a given job:
/// Parse → [Group] → Map → Validate → [Resolve Identity] → Execute.
/// </summary>
/// <remarks>
/// Invoked by the Wolverine handler for <see cref="Messages.ExecuteImportCommand"/>.
/// Coordinates all pipeline steps and builds the final <see cref="ImportReport"/>.
/// </remarks>
public interface IImportOrchestrator
{
    /// <summary>
    /// Executes the full import pipeline for the given job.
    /// </summary>
    /// <param name="importJobId">The import job identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The import report with statistics and row-level errors.</returns>
    Task<ImportReport> ExecuteAsync(
        Guid importJobId,
        CancellationToken ct = default);

    /// <summary>
    /// Executes the import pipeline in dry-run mode (no data persisted, transaction rolled back).
    /// </summary>
    /// <param name="importJobId">The import job identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The import report with validation results but no actual persistence.</returns>
    Task<ImportReport> DryRunAsync(
        Guid importJobId,
        CancellationToken ct = default);
}
