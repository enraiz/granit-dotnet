using Granit.BackgroundJobs.Abstractions;
using Wolverine;

namespace Granit.BackgroundJobs.Wolverine.Internal;

/// <summary>
/// <see cref="IBackgroundJobDispatcher"/> implementation backed by Wolverine's
/// <see cref="IMessageBus"/> for durable, transactional dispatch.
/// </summary>
internal sealed class WolverineBackgroundJobDispatcher(IMessageBus bus) : IBackgroundJobDispatcher
{
    /// <inheritdoc/>
    public async Task PublishAsync(
        object message,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        if (headers is { Count: > 0 })
        {
            DeliveryOptions options = new();
            foreach ((string key, string value) in headers)
            {
                options.Headers[key] = value;
            }

            await bus.PublishAsync(message, options).ConfigureAwait(false);
        }
        else
        {
            await bus.PublishAsync(message).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task ScheduleAsync(
        object message,
        DateTimeOffset scheduledTime,
        CancellationToken cancellationToken = default) =>
        await bus.ScheduleAsync(message, scheduledTime).ConfigureAwait(false);
}
