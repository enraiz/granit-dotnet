using System.Threading.Channels;
using Granit.BackgroundJobs.Abstractions;

namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Default <see cref="IBackgroundJobDispatcher"/> implementation that writes messages
/// to an in-process <see cref="Channel{T}"/> consumed by <see cref="BackgroundJobWorker"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PublishAsync"/> writes immediately to the channel.
/// <see cref="ScheduleAsync"/> uses <see cref="Task.Delay(TimeSpan, TimeProvider, CancellationToken)"/>
/// to defer the write until the scheduled time.
/// </para>
/// <para>
/// The delayed scheduling does not survive process restarts. For durable scheduling,
/// use <c>Granit.BackgroundJobs.Wolverine</c>.
/// </para>
/// </remarks>
internal sealed class ChannelBackgroundJobDispatcher(
    Channel<BackgroundJobEnvelope> channel,
    TimeProvider timeProvider) : IBackgroundJobDispatcher
{
    /// <inheritdoc/>
    public async Task PublishAsync(
        object message,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) =>
        await channel.Writer.WriteAsync(new BackgroundJobEnvelope(message, headers), cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task ScheduleAsync(
        object message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default)
    {
        TimeSpan delay = scheduledTime - timeProvider.GetUtcNow();
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, timeProvider, cancellationToken).ConfigureAwait(false);
        }

        await channel.Writer.WriteAsync(new BackgroundJobEnvelope(message), cancellationToken)
            .ConfigureAwait(false);
    }
}
