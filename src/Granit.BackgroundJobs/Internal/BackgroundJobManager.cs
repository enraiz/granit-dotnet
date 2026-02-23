using Cronos;
using Granit.Core.Exceptions;
using Granit.Security;
using Granit.Timing;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Default implementation of <see cref="IBackgroundJobManager"/>.
/// Registered as <b>Scoped</b> in the DI container.
/// </summary>
internal sealed partial class BackgroundJobManager(
    IBackgroundJobStore store,
    IMessageBus bus,
    IClock clock,
    ICurrentUserService currentUserService,
    ILogger<BackgroundJobManager> logger) : IBackgroundJobManager
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<BackgroundJobStatus>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<BackgroundJobDefinition> jobs = await store.GetAllJobsAsync(ct);
        return jobs.Select(ToStatus).ToList();
    }

    /// <inheritdoc/>
    public async Task<BackgroundJobStatus?> FindAsync(string jobName, CancellationToken ct = default)
    {
        BackgroundJobDefinition? job = await store.FindAsync(jobName, ct);
        return job is null ? null : ToStatus(job);
    }

    /// <inheritdoc/>
    public async Task PauseAsync(string jobName, CancellationToken ct = default)
    {
        BackgroundJobDefinition job = await RequireJobAsync(jobName, ct);
        await store.SetEnabledAsync(job.JobName, false, ct);
        LogJobPaused(logger, jobName);
    }

    /// <inheritdoc/>
    public async Task ResumeAsync(string jobName, CancellationToken ct = default)
    {
        BackgroundJobDefinition job = await RequireJobAsync(jobName, ct);
        await store.SetEnabledAsync(job.JobName, true, ct);

        DateTimeOffset? next = ComputeNext(job.CronExpression);
        if (next is not null)
        {
            object message = CreateMessage(job.MessageType, jobName);
            await bus.ScheduleAsync(message, next.Value);
            await store.RecordNextExecutionAsync(job.JobName, next.Value, ct);
            LogJobResumed(logger, jobName, next.Value);
        }
        else
        {
            LogJobResumedNoCron(logger, jobName, job.CronExpression);
        }
    }

    /// <inheritdoc/>
    public async Task TriggerNowAsync(string jobName, CancellationToken ct = default)
    {
        BackgroundJobDefinition job = await RequireJobAsync(jobName, ct);
        object message = CreateMessage(job.MessageType, jobName);

        DeliveryOptions options = new();
        if (currentUserService.IsAuthenticated
            && currentUserService.UserId is { Length: > 0 } userId)
        {
            options.Headers[RecurringJobSchedulingMiddleware.TriggeredByHeader] = userId;
        }

        await bus.PublishAsync(message, options);
        LogJobTriggered(logger, jobName, currentUserService.UserId ?? "system");
    }

    private async Task<BackgroundJobDefinition> RequireJobAsync(
        string jobName,
        CancellationToken ct)
    {
        BackgroundJobDefinition? job = await store.FindAsync(jobName, ct);
        if (job is null)
        {
            throw new EntityNotFoundException(typeof(BackgroundJobDefinition), jobName);
        }

        return job;
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
        Type? type = Type.GetType(messageType);
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

    private static BackgroundJobStatus ToStatus(BackgroundJobDefinition job) =>
        new(
            JobName: job.JobName,
            CronExpression: job.CronExpression,
            IsEnabled: job.IsEnabled,
            LastExecutedAt: job.LastExecutedAt,
            NextExecutionAt: job.NextExecutionAt,
            ConsecutiveFailures: job.ConsecutiveFailureCount,
            DeadLetterCount: 0, // DLQ stats: Story #134 (requires IMessageStore)
            LastError: job.LastErrorMessage);

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
}
