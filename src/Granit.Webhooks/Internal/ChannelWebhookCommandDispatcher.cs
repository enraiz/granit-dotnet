using System.Threading.Channels;
using Granit.Webhooks.Abstractions;
using Granit.Webhooks.Messages;

namespace Granit.Webhooks.Internal;

/// <summary>
/// Default <see cref="IWebhookCommandDispatcher"/> implementation that writes
/// <see cref="SendWebhookCommand"/> messages to an in-process channel.
/// </summary>
internal sealed class ChannelWebhookCommandDispatcher(
    Channel<SendWebhookCommand> channel) : IWebhookCommandDispatcher
{
    /// <inheritdoc/>
    public async Task DispatchAsync(SendWebhookCommand command, CancellationToken cancellationToken = default) =>
        await channel.Writer.WriteAsync(command, cancellationToken).ConfigureAwait(false);
}
