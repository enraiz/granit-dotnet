namespace Granit.DataExchange.Export;

/// <summary>
/// Result returned by <see cref="IExportOrchestrator.ExportAsync"/>.
/// </summary>
/// <param name="JobId">The export job identifier.</param>
/// <param name="Status">
/// <see cref="ExportJobStatus.Completed"/> for synchronous (small) exports,
/// <see cref="ExportJobStatus.Queued"/> for background (large) exports.
/// </param>
public sealed record ExportJobResult(
    Guid JobId,
    ExportJobStatus Status);
