namespace Granit.DataExchange.Export;

/// <summary>
/// Orchestrates data export: resolves the definition, queries data via
/// <see cref="IExportDataSource{TEntity,TFilter}"/>, projects fields,
/// and writes the output file via <see cref="IExportWriter"/>.
/// </summary>
/// <remarks>
/// <para>
/// For small datasets (below the configured threshold), the export runs synchronously
/// and the file is returned immediately.
/// </para>
/// <para>
/// For large datasets (above the threshold), the export is dispatched to a background
/// job via <see cref="IExportCommandDispatcher"/> and the caller polls for completion.
/// </para>
/// </remarks>
public interface IExportOrchestrator
{
    /// <summary>
    /// Starts an export job. Returns immediately with the job ID and initial status.
    /// </summary>
    /// <param name="request">The export configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A result with <see cref="ExportJobStatus.Completed"/> if the export was synchronous,
    /// or <see cref="ExportJobStatus.Queued"/> if dispatched to background.
    /// </returns>
    Task<ExportJobResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the export for a previously created job (called by the background worker).
    /// </summary>
    /// <param name="jobId">The export job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status and metadata of an export job.
    /// </summary>
    /// <param name="jobId">The export job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ExportJob?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the download stream for a completed export job.
    /// </summary>
    /// <param name="jobId">The export job identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file stream and metadata, or <c>null</c> if not found or not completed.</returns>
    Task<ExportDownload?> GetDownloadAsync(Guid jobId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Download result for a completed export.
/// </summary>
/// <param name="Content">The file stream.</param>
/// <param name="MimeType">The MIME type.</param>
/// <param name="FileName">The suggested file name.</param>
public sealed record ExportDownload(
    Stream Content,
    string MimeType,
    string FileName);
