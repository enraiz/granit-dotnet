using Granit.DataExchange.Import.Domain;

namespace Granit.DataExchange.Import.Pipeline;

/// <summary>
/// Persists <see cref="ImportJob"/> entities.
/// </summary>
public interface IImportJobWriter
{
    /// <summary>
    /// Creates a new import job.
    /// </summary>
    /// <param name="job">The import job to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateAsync(ImportJob job, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing import job.
    /// </summary>
    /// <param name="job">The import job with updated state.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(ImportJob job, CancellationToken ct = default);
}
