using Granit.DataExchange.Import.Domain;

namespace Granit.DataExchange.Endpoints.Dtos.Import;

/// <summary>
/// Response DTO for an import job summary.
/// </summary>
public sealed record ImportJobResponse(
    Guid Id,
    string DefinitionName,
    string OriginalFileName,
    string MimeType,
    long FileSizeBytes,
    ImportJobStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt)
{
    /// <summary>
    /// Maps an <see cref="ImportJob"/> domain entity to a response DTO.
    /// </summary>
    internal static ImportJobResponse FromJob(ImportJob job) =>
        new(job.Id, job.DefinitionName, job.OriginalFileName, job.MimeType,
            job.FileSizeBytes, job.Status, job.CreatedAt, job.CompletedAt);
}
