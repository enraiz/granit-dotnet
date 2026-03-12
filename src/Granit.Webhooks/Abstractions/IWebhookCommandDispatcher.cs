using Granit.Webhooks.Messages;

namespace Granit.Webhooks.Abstractions;

/// <summary>
/// Dispatches <see cref="SendWebhookCommand"/> for delivery.
/// </summary>
/// <remarks>
/// The default in-process implementation writes to a <see cref="System.Threading.Channels.Channel{T}"/>.
/// When <c>Granit.Webhooks.Wolverine</c> is loaded, this is replaced with an
/// <c>IMessageBus.SendAsync</c> implementation for durable dispatch.
/// </remarks>
public interface IWebhookCommandDispatcher
{
    /// <summary>
    /// Dispatches a single webhook delivery command.
    /// </summary>
    Task DispatchAsync(SendWebhookCommand command, CancellationToken cancellationToken = default);
}
