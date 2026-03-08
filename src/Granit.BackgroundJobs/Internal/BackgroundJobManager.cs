using Cronos;
using Granit.Core.Exceptions;
using Granit.Security;
using Granit.Timing;
using JasperFx.Core;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.Persistence.Durability;
using Wolverine.Persistence.Durability.DeadLetterManagement;

namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Default implementation of <see cref="IBackgroundJobReader"/> and <see cref="IBackgroundJobWriter"/>.
/// Registered as <b>Scoped</b> in the DI container.
/// </summary>
internal sealed partial class BackgroundJobManager(
    IBackgroundJobStoreReader storeReader,
    IBackgroundJobStoreWriter storeWriter,
    IMessageBus bus,
    IClock clock,
    ICurrentUserService currentUserService,
    ILogger<BackgroundJobManager> logger,
    IMessageStore? messageStore = null) : IBackgroundJobReader, IBackgroundJobWriter
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<BackgroundJobStatus>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BackgroundJobDefinition> jobs = await storeReader.GetAllJobsAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<string, long> dlqCounts = await GetDlqCountsAsync(cancellationToken).ConfigureAwait(false);
        return jobs.Select(j => ToStatus(j, dlqCounts)).ToList();
    }

    /// <inheritdoc/>
    public async Task<BackgroundJobStatus?> FindAsync(string jobName, CancellationToken cancellationToken = default)
    {
        BackgroundJobDefinition? job = await storeReader.FindAsync(jobName, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            return null;
        }

        Dictionary<string, long> dlqCounts = await GetDlqCountsAsync(cancellationToken).ConfigureAwait(false);
        return ToStatus(job, dlqCounts);
    }

    /// <inheritdoc/>
    public async Task PauseAsync(string jobName, CancellationToken cancellationToken = default)
    {
        BackgroundJobDefinition job = await RequireJobAsync(jobName, cancellationToken).ConfigureAwait(false);
        await storeWriter.SetEnabledAsync(job.JobName, false, cancellationToken).ConfigureAwait(false);
        LogJobPaused(logger, jobName);
    }

    /// <inheritdoc/>
    public async Task ResumeAsync(string jobName, CancellationToken cancellationToken = default)
    {
        BackgroundJobDefinition job = await RequireJobAsync(jobName, cancellationToken).ConfigureAwait(false);
        await storeWriter.SetEnabledAsync(job.JobName, true, cancellationToken).ConfigureAwait(false);

        DateTimeOffset? next = ComputeNext(job.CronExpression);
        if (next is not null)
        {
            object message = CreateMessage(job.MessageType, jobName);
            await bus.ScheduleAsync(message, next.Value).ConfigureAwait(false);
            await storeWriter.RecordNextExecutionAsync(job.JobName, next.Value, cancellationToken).ConfigureAwait(false);
            LogJobResumed(logger, jobName, next.Value);
        }
        else
        {
            LogJobResumedNoCron(logger, jobName, job.CronExpression);
        }
    }

    /// <inheritdoc/>
    public async Task TriggerNowAsync(string jobName, CancellationToken cancellationToken = default)
    {
        BackgroundJobDefinition job = await RequireJobAsync(jobName, cancellationToken).ConfigureAwait(false);
        object message = CreateMessage(job.MessageType, jobName);

        DeliveryOptions options = new();
        if (currentUserService.IsAuthenticated
            && currentUserService.UserId is { Length: > 0 } userId)
        {
            options.Headers[RecurringJobSchedulingMiddleware.TriggeredByHeader] = userId;
        }

        await bus.PublishAsync(message, options).ConfigureAwait(false);
        LogJobTriggered(logger, jobName, currentUserService.UserId ?? "system");
    }

    private async Task<BackgroundJobDefinition> RequireJobAsync(
        string jobName,
        CancellationToken cancellationToken)
    {
        BackgroundJobDefinition? job = await storeReader.FindAsync(jobName, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            throw new EntityNotFoundException(typeof(BackgroundJobDefinition), jobName);
        }

        return job;
    }

    /// <summary>
    /// Fetches Dead Letter Queue counts from the Wolverine message store.
    /// Returns an empty dictionary if the store is unavailable or the query fails,
    /// ensuring graceful degradation (DeadLetterCount = 0) without crashing.
    /// </summary>
    private async Task<Dictionary<string, long>> GetDlqCountsAsync(CancellationToken cancellationToken)
    {
        if (messageStore is null)
        {
            return [];
        }

        try
        {
            IReadOnlyList<DeadLetterQueueCount> counts =
                await messageStore.DeadLetters.SummarizeAllAsync(
                    string.Empty, TimeRange.AllTime(), cancellationToken).ConfigureAwait(false);

            return counts.ToDictionary(
                c => c.MessageType,
                c => (long)c.Count,
                StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            LogDlqQueryFailed(logger, ex);
            return [];
        }
    }

    private static BackgroundJobStatus ToStatus(
        BackgroundJobDefinition job,
        Dictionary<string, long> dlqCounts)
    {
        // BackgroundJobDefinition.MessageType is assembly-qualified (e.g. "MyApp.MyMsg, MyApp, ...").
        // DeadLetterQueueCount.MessageType contains only the type's full name (no assembly suffix).
        string shortTypeName = job.MessageType.Split(',')[0].Trim();
        long dlqCount = dlqCounts.GetValueOrDefault(shortTypeName);

        return new(
            JobName: job.JobName,
            CronExpression: job.CronExpression,
            IsEnabled: job.IsEnabled,
            LastExecutedAt: job.LastExecutedAt,
            NextExecutionAt: job.NextExecutionAt,
            ConsecutiveFailures: job.ConsecutiveFailureCount,
            DeadLetterCount: dlqCount,
            LastError: job.LastErrorMessage);
    }

    private DateTimeOffset? ComputeNext(string cronExpression)
    {
        try
        {
            CronExpression cron;
            try
            {
                cron = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
            }
            catch (CronFormatException)
            {
                cron = CronExpression.Parse(cronExpression);
            }

            return cron.GetNextOccurrence(clock.Now, TimeZoneInfo.Utc);
        }
        catch (CronFormatException)
        {
            return null;
        }
    }

    private static object CreateMessage(string messageType, string jobName)
    {
        var type = Type.GetType(messageType);
        if (type is null)
        {
            throw new InvalidOperationException(
                $"Cannot resolve message type '{messageType}' for job '{jobName}'. " +
                "Ensure the assembly containing the message is loaded.");
        }

        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException(
                $"Cannot instantiate message type '{type.Name}' for job '{jobName}'.");
    }

    // =========================================================================
    // Source-generated logger messages (CA1873 / CA1848 compliance)
    // =========================================================================

    [LoggerMessage(Level = LogLevel.Information, Message = "BackgroundJob '{JobName}' paused.")]
    private static partial void LogJobPaused(ILogger logger, string jobName);

    [LoggerMessage(Level = LogLevel.Information, Message = "BackgroundJob '{JobName}' resumed. Next execution: {Next}.")]
    private static partial void LogJobResumed(ILogger logger, string jobName, DateTimeOffset next);

    [LoggerMessage(Level = LogLevel.Warning, Message = "BackgroundJob '{JobName}' resumed but cron '{Cron}' produced no next occurrence.")]
    private static partial void LogJobResumedNoCron(ILogger logger, string jobName, string cron);

    [LoggerMessage(Level = LogLevel.Information, Message = "BackgroundJob '{JobName}' triggered manually by '{UserId}'.")]
    private static partial void LogJobTriggered(ILogger logger, string jobName, string userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to query Wolverine Dead Letter Queue; DeadLetterCount will be 0 for all jobs.")]
    private static partial void LogDlqQueryFailed(ILogger logger, Exception exception);
}
