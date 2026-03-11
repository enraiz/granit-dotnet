using Granit.DataExchange.Export.Domain;

namespace Granit.DataExchange.Export;

/// <summary>
/// Persists export job entities.
/// </summary>
public interface IExportJobWriter
{
    /// <summary>
    /// Creates a new export job.
    /// </summary>
    Task CreateAsync(ExportJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing export job.
    /// </summary>
    Task UpdateAsync(ExportJob job, CancellationToken cancellationToken = default);
}
