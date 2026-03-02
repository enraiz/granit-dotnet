using Granit.DataImport.Domain;

namespace Granit.DataImport.Pipeline;

/// <summary>
/// Persists and retrieves <see cref="ImportJob"/> entities.
/// </summary>
/// <remarks>
/// The default registration is a null-object that throws <see cref="NotImplementedException"/>.
/// A concrete implementation is provided by <c>Granit.DataImport.EntityFrameworkCore</c>.
/// </remarks>
public interface IImportJobStore
{
    /// <summary>
    /// Loads an import job by identifier.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The import job, or <c>null</c> if not found.</returns>
    Task<ImportJob?> GetAsync(Guid id, CancellationToken ct = default);

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
