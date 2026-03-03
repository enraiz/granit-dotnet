using Granit.DataExchange.Import.Domain;
using Granit.DataExchange.Import.Reporting;

namespace Granit.DataExchange.Endpoints.Dtos.Import;

/// <summary>
/// Response DTO for an import execution report.
/// </summary>
public sealed record ImportReportResponse(
    Guid ImportJobId,
    ImportJobStatus FinalStatus,
    int TotalRows,
    int SucceededRows,
    int FailedRows,
    int SkippedRows,
    int InsertedRows,
    int UpdatedRows,
    TimeSpan Duration,
    IReadOnlyList<ImportRowError> RowErrors)
{
    /// <summary>
    /// Maps an <see cref="ImportReport"/> domain model to a response DTO.
    /// </summary>
    internal static ImportReportResponse FromReport(Guid importJobId, ImportReport report) =>
        new(importJobId, report.FinalStatus, report.TotalRows, report.SucceededRows,
            report.FailedRows, report.SkippedRows, report.InsertedRows, report.UpdatedRows,
            report.Duration, report.RowErrors);
}
