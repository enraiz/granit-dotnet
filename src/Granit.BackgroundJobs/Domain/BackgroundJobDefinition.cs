using Granit.BackgroundJobs.Events;
using Granit.BackgroundJobs.Internal;
using Granit.Core.Domain;
using Granit.Core.Events;
namespace Granit.BackgroundJobs.Domain;

/// <summary>
/// Persistent administrative record for a Wolverine recurring job.
/// </summary>
/// <remarks>
/// <para>
/// This entity tracks the administrative state of a job — not the Wolverine messages themselves
/// (those live in the Outbox). It stores scheduling metadata, execution history, and pause/resume
/// state that persists across application restarts.
/// </para>
/// <para>
/// HDS compliance: <see cref="LastExecutedAt"/> and <see cref="TriggeredBy"/> are write-once
/// per execution cycle and preserved for audit purposes.
/// </para>
/// </remarks>
public sealed class BackgroundJobDefinition : Entity, IDomainEventSource
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Unique, stable job name matching <see cref="RecurringJobAttribute.Name"/>.
    /// Primary lookup key. Maximum length: 200 characters.
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Assembly-qualified CLR type name of the Wolverine message.
    /// Used by <see cref="IBackgroundJobWriter.TriggerNowAsync"/> to instantiate the message.
    /// Maximum length: 500 characters.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Cron expression (5 or 6 fields) defining the recurrence schedule.
    /// Updated on each deployment if the expression changes in code.
    /// Maximum length: 100 characters.
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Whether the job is active. When <c>false</c>, the scheduling middleware
    /// skips rescheduling after each execution (Pause).
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>UTC timestamp of the last successful execution start.</summary>
    public DateTimeOffset? LastExecutedAt { get; set; }

    /// <summary>UTC timestamp of the next scheduled execution (set by the middleware).</summary>
    public DateTimeOffset? NextExecutionAt { get; set; }

    /// <summary>
    /// Number of consecutive handler failures since the last successful execution.
    /// Reset to zero on success.
    /// </summary>
    public int ConsecutiveFailureCount { get; set; }

    /// <summary>
    /// Error message from the last handler failure.
    /// Maximum length: 2000 characters. Null when the last execution succeeded.
    /// </summary>
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// UserId (not PII) of the operator who manually triggered this job via
    /// <see cref="IBackgroundJobWriter.TriggerNowAsync"/>.
    /// Null for scheduled executions. HDS audit field.
    /// Maximum length: 450 characters.
    /// </summary>
    public string? TriggeredBy { get; set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Pauses the job and emits a <see cref="BackgroundJobPaused"/> domain event.
    /// </summary>
    internal void Pause()
    {
        IsEnabled = false;
        _domainEvents.Add(new BackgroundJobPaused(Id, JobName));
    }

    /// <summary>
    /// Resumes the job and emits a <see cref="BackgroundJobResumed"/> domain event.
    /// </summary>
    internal void Resume()
    {
        IsEnabled = true;
        _domainEvents.Add(new BackgroundJobResumed(Id, JobName));
    }
}
