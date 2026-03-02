namespace Granit.DataImport.Reporting;

/// <summary>
/// Progress update during import execution.
/// </summary>
/// <param name="ProcessedRows">Total rows processed so far.</param>
/// <param name="TotalRows">Estimated total rows (may be 0 if unknown).</param>
/// <param name="SucceededRows">Rows successfully imported.</param>
/// <param name="FailedRows">Rows that failed.</param>
public sealed record ImportProgress(
    int ProcessedRows,
    int TotalRows,
    int SucceededRows,
    int FailedRows);
