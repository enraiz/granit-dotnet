using Granit.Webhooks.Abstractions;
using Granit.Webhooks.Messages;
using Wolverine;

namespace Granit.Webhooks.Wolverine.Internal;

/// <summary>
/// <see cref="IWebhookCommandDispatcher"/> implementation that dispatches via
/// Wolverine's <see cref="IMessageBus"/> for durable delivery.
/// </summary>
internal sealed class WolverineWebhookCommandDispatcher(IMessageBus bus) : IWebhookCommandDispatcher
{
    /// <inheritdoc/>
    public async Task DispatchAsync(SendWebhookCommand command, CancellationToken cancellationToken = default) =>
        await bus.SendAsync(command).ConfigureAwait(false);
}
