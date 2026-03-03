using Granit.DataImport.Export;

namespace Granit.DataImport.Endpoints.Dtos;

/// <summary>
/// Response DTO for an export job summary.
/// </summary>
public sealed record ExportJobResponse(
    Guid Id,
    string DefinitionName,
    string Format,
    ExportJobStatus Status,
    int? RowCount,
    string? FileName,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt)
{
    /// <summary>
    /// Maps an <see cref="ExportJob"/> domain entity to a response DTO.
    /// </summary>
    internal static ExportJobResponse FromJob(ExportJob job) =>
        new(job.Id, job.DefinitionName, job.Format, job.Status, job.RowCount,
            job.FileName, job.ErrorMessage, job.CreatedAt, job.CompletedAt);
}
