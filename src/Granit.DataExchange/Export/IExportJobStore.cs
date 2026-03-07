using Granit.Querying;

namespace Granit.DataExchange.Export;

/// <summary>
/// Stores and retrieves export job entities.
/// </summary>
/// <remarks>
/// The default registration is a null-object that throws <see cref="NotSupportedException"/>.
/// Install <c>Granit.DataExchange.EntityFrameworkCore</c> for EF Core-backed persistence.
/// </remarks>
public interface IExportJobStore
{
    /// <summary>
    /// Gets an export job by ID.
    /// </summary>
    Task<ExportJob?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lists export jobs with optional status filter and offset pagination.
    /// Results are ordered by <see cref="ExportJob.CreatedAt"/> descending.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="page">One-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PagedResult<ExportJob>> ListAsync(
        ExportJobStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new export job.
    /// </summary>
    Task CreateAsync(ExportJob job, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing export job.
    /// </summary>
    Task UpdateAsync(ExportJob job, CancellationToken ct = default);
}
