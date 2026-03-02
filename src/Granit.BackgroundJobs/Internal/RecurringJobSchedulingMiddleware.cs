using System.Reflection;
using Cronos;
using Granit.Timing;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Wolverine middleware that records job execution and atomically reschedules the next
/// occurrence inside the Outbox transaction after each successful handler invocation.
/// </summary>
/// <remarks>
/// <para>
/// Injected automatically via <c>opts.Policies.AddMiddleware</c> on all handler chains
/// whose message type carries <see cref="RecurringJobAttribute"/>. Never applied manually.
/// </para>
/// <para>
/// <b>Anti-doublon guarantee:</b> <see cref="AfterAsync"/> calls
/// <see cref="IMessageContext.ScheduleAsync"/> inside the same database transaction as
/// the handler. If the node crashes before the transaction commits, Wolverine redelivers
/// the current message — the "next" message was never inserted, so no duplicate is created.
/// </para>
/// <para>
/// <b>HDS audit:</b> <see cref="BeforeAsync"/> reads the <c>X-Triggered-By</c> header
/// (set by <see cref="IBackgroundJobManager.TriggerNowAsync"/>) and persists it via
/// <see cref="IBackgroundJobStore.SetTriggeredByAsync"/>.
/// </para>
/// </remarks>
internal sealed partial class RecurringJobSchedulingMiddleware(
    IBackgroundJobStore store,
    IClock clock,
    ILogger<RecurringJobSchedulingMiddleware> logger)
{
    /// <summary>Header name carrying the admin identity for manual triggers (HDS audit).</summary>
    internal const string TriggeredByHeader = "X-Triggered-By";

    /// <summary>
    /// Records the execution start and captures the <c>X-Triggered-By</c> header for HDS audit.
    /// </summary>
    public async Task BeforeAsync(Envelope envelope, CancellationToken ct)
    {
        RecurringJobAttribute? attr = envelope.Message?.GetType()
            .GetCustomAttribute<RecurringJobAttribute>();

        if (attr is null)
        {
            return;
        }

        await store.RecordExecutionStartAsync(attr.Name, clock.Now, ct).ConfigureAwait(false);

        if (envelope.Headers.TryGetValue(TriggeredByHeader, out string? triggeredBy)
            && !string.IsNullOrEmpty(triggeredBy))
        {
            await store.SetTriggeredByAsync(attr.Name, triggeredBy, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Calculates the next cron occurrence and schedules the message inside the Outbox
    /// transaction. Skipped when the job is paused (<see cref="BackgroundJobDefinition.IsEnabled"/> = <c>false</c>).
    /// </summary>
    public async Task AfterAsync(Envelope envelope, IMessageContext context, CancellationToken ct)
    {
        RecurringJobAttribute? attr = envelope.Message?.GetType()
            .GetCustomAttribute<RecurringJobAttribute>();

        if (attr is null)
        {
            return;
        }

        BackgroundJobDefinition? job = await store.FindAsync(attr.Name, ct).ConfigureAwait(false);
        if (job is not { IsEnabled: true })
        {
            return;
        }

        CronExpression cron;
        try
        {
            cron = CronExpression.Parse(job.CronExpression, CronFormat.IncludeSeconds);
        }
        catch (CronFormatException)
        {
            // Fall back to standard 5-field format.
            cron = CronExpression.Parse(job.CronExpression);
        }

        DateTimeOffset? next = cron.GetNextOccurrence(clock.Now, TimeZoneInfo.Utc);
        if (next is null)
        {
            LogNoCronOccurrence(logger, job.JobName, job.CronExpression);
            return;
        }

        object nextMessage = Activator.CreateInstance(envelope.Message!.GetType())!;
        await context.ScheduleAsync(nextMessage, next.Value).ConfigureAwait(false);
        await store.RecordNextExecutionAsync(job.JobName, next.Value, ct).ConfigureAwait(false);
    }

    // =========================================================================
    // Source-generated logger messages (CA1873 / CA1848 compliance)
    // =========================================================================

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "RecurringJob '{JobName}': cron expression '{Cron}' produced no next occurrence.")]
    private static partial void LogNoCronOccurrence(ILogger logger, string jobName, string cron);
}
