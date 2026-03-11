using Granit.Webhooks.Abstractions;
using Granit.Webhooks.Domain;

namespace Granit.Webhooks.Internal;

/// <summary>
/// No-op implementation of <see cref="IWebhookDeliveryReader"/> that always returns <c>null</c>.
/// Suitable for development and tests where delivery persistence is not available.
/// </summary>
internal sealed class NullWebhookDeliveryReader : IWebhookDeliveryReader
{
    public Task<WebhookDeliveryAttempt?> FindByDeliveryIdAsync(Guid deliveryId, CancellationToken cancellationToken = default) =>
        Task.FromResult<WebhookDeliveryAttempt?>(null);

    public Task<int> CountBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}
