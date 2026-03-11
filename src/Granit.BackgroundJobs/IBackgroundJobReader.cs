namespace Granit.BackgroundJobs;

/// <summary>
/// Read operations for background job status monitoring.
/// </summary>
public interface IBackgroundJobReader
{
    /// <summary>Returns the current status of all registered recurring jobs.</summary>
    Task<IReadOnlyList<BackgroundJobStatus>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the status of a specific job, or <c>null</c> if not found.
    /// </summary>
    Task<BackgroundJobStatus?> FindAsync(string jobName, CancellationToken cancellationToken = default);
}
