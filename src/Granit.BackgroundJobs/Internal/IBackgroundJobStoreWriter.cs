namespace Granit.BackgroundJobs;

/// <summary>
/// Write operations for the background job store.
/// </summary>
public interface IBackgroundJobStoreWriter
{
    /// <summary>
    /// Inserts new jobs and updates <see cref="BackgroundJobDefinition.CronExpression"/>
    /// for existing ones. Administrative state (IsEnabled, TriggeredBy) is preserved.
    /// Idempotent: safe to call on every startup.
    /// </summary>
    Task SeedJobsAsync(IEnumerable<RecurringJobRegistration> registrations, CancellationToken ct = default);

    /// <summary>Records the start of an execution and clears the last error.</summary>
    Task RecordExecutionStartAsync(string jobName, DateTimeOffset startedAt, CancellationToken ct = default);

    /// <summary>Updates the next scheduled execution time.</summary>
    Task RecordNextExecutionAsync(string jobName, DateTimeOffset nextExecution, CancellationToken ct = default);

    /// <summary>Increments the failure counter and stores the error message.</summary>
    Task RecordExecutionFailureAsync(string jobName, string errorMessage, CancellationToken ct = default);

    /// <summary>Sets <see cref="BackgroundJobDefinition.IsEnabled"/>.</summary>
    Task SetEnabledAsync(string jobName, bool enabled, CancellationToken ct = default);

    /// <summary>Sets <see cref="BackgroundJobDefinition.TriggeredBy"/> for HDS audit.</summary>
    Task SetTriggeredByAsync(string jobName, string? triggeredBy, CancellationToken ct = default);
}
