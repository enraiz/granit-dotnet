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
    /// Creates a new export job.
    /// </summary>
    Task CreateAsync(ExportJob job, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing export job.
    /// </summary>
    Task UpdateAsync(ExportJob job, CancellationToken ct = default);
}
