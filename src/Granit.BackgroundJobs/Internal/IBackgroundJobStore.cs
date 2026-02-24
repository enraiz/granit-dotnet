namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Internal abstraction for reading and updating the administrative job store.
/// Implemented by <see cref="InMemoryBackgroundJobStore"/> and <c>EfBackgroundJobStore</c>.
/// </summary>
internal interface IBackgroundJobStore
{
    /// <summary>Returns a job by name, or <c>null</c> if not found.</summary>
    Task<BackgroundJobDefinition?> FindAsync(string jobName, CancellationToken ct = default);

    /// <summary>Returns all jobs with <see cref="BackgroundJobDefinition.IsEnabled"/> = <c>true</c>.</summary>
    Task<IReadOnlyList<BackgroundJobDefinition>> GetEnabledJobsAsync(CancellationToken ct = default);

    /// <summary>Returns all jobs regardless of enabled state.</summary>
    Task<IReadOnlyList<BackgroundJobDefinition>> GetAllJobsAsync(CancellationToken ct = default);

    /// <summary>
    /// Inserts new jobs and updates <see cref="BackgroundJobDefinition.CronExpression"/>
    /// for existing ones. Administrative state (IsEnabled, TriggeredBy) is preserved.
    /// Idempotent: safe to call on every startup.
    /// </summary>
    Task SeedJobsAsync(IEnumerable<RecurringJobRegistration> registrations, CancellationToken ct = default);

    /// <summary>Records the start of an execution and clears the last error.</summary>
    Task RecordExecutionStartAsync(string jobName, DateTimeOffset startedAt, CancellationToken ct = default);

    /// <summary>Updates the next scheduled execution time.</summary>
    Task RecordNextExecutionAsync(string jobName, DateTimeOffset next, CancellationToken ct = default);

    /// <summary>Increments the failure counter and stores the error message.</summary>
    Task RecordExecutionFailureAsync(string jobName, string errorMessage, CancellationToken ct = default);

    /// <summary>Sets <see cref="BackgroundJobDefinition.IsEnabled"/>.</summary>
    Task SetEnabledAsync(string jobName, bool enabled, CancellationToken ct = default);

    /// <summary>Sets <see cref="BackgroundJobDefinition.TriggeredBy"/> for HDS audit.</summary>
    Task SetTriggeredByAsync(string jobName, string? triggeredBy, CancellationToken ct = default);
}
