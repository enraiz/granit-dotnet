using System.Text.Json;
using Granit.Core.MultiTenancy;
using Granit.Timing;
using Granit.Webhooks.Abstractions;
using Granit.Webhooks.Messages;
using Wolverine;

namespace Granit.Webhooks.Internal;

/// <summary>
/// Default implementation of <see cref="IWebhookPublisher"/> backed by Wolverine.
/// </summary>
/// <remarks>
/// Serializes the payload to a <see cref="JsonElement"/> and publishes a
/// <see cref="WebhookTrigger"/> into the Wolverine Outbox. The ambient
/// <see cref="ICurrentTenant"/> is captured when available (<c>IsAvailable = true</c>).
/// </remarks>
internal sealed class WolverineWebhookPublisher(IMessageBus bus, ICurrentTenant currentTenant, IClock clock) : IWebhookPublisher
{
    public async ValueTask PublishAsync<TPayload>(
        string eventType,
        TPayload payload,
        CancellationToken cancellationToken = default) where TPayload : notnull
    {
        JsonElement serializedPayload = JsonSerializer.SerializeToElement(payload);

        WebhookTrigger trigger = new()
        {
            EventType = eventType,
            Payload = serializedPayload,
            TenantId = currentTenant.IsAvailable ? currentTenant.Id : null,
            OccurredAt = clock.Now,
        };

        await bus.PublishAsync(trigger).ConfigureAwait(false);
    }
}
