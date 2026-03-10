using Granit.BackgroundJobs.Domain;

namespace Granit.BackgroundJobs;

/// <summary>
/// Read operations for the background job store.
/// </summary>
public interface IBackgroundJobStoreReader
{
    /// <summary>Returns a job by name, or <c>null</c> if not found.</summary>
    Task<BackgroundJobDefinition?> FindAsync(string jobName, CancellationToken cancellationToken = default);

    /// <summary>Returns all jobs with <see cref="BackgroundJobDefinition.IsEnabled"/> = <c>true</c>.</summary>
    Task<IReadOnlyList<BackgroundJobDefinition>> GetEnabledJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all jobs regardless of enabled state.</summary>
    Task<IReadOnlyList<BackgroundJobDefinition>> GetAllJobsAsync(CancellationToken cancellationToken = default);
}
