namespace Granit.BackgroundJobs.Abstractions;

/// <summary>
/// Dispatches background job messages for execution.
/// </summary>
/// <remarks>
/// The default implementation writes to an in-process <see cref="System.Threading.Channels.Channel{T}"/>.
/// When <c>Granit.BackgroundJobs.Wolverine</c> is loaded, this is replaced with an
/// <c>IMessageBus</c>-backed implementation for durable, transactional dispatch.
/// </remarks>
public interface IBackgroundJobDispatcher
{
    /// <summary>
    /// Publishes a message for immediate processing.
    /// </summary>
    /// <param name="message">The job message instance.</param>
    /// <param name="headers">Optional headers (e.g. <c>X-Triggered-By</c> for audit).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(object message, IDictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a message for delayed processing at the specified time.
    /// </summary>
    /// <param name="message">The job message instance.</param>
    /// <param name="scheduledTime">The UTC time at which the message should be processed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ScheduleAsync(object message, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default);
}
