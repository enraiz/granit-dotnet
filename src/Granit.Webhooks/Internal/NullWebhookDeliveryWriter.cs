using Granit.Webhooks.Abstractions;
using Granit.Webhooks.Messages;

namespace Granit.Webhooks.Internal;

/// <summary>
/// No-op implementation of <see cref="IWebhookDeliveryWriter"/> that discards all records.
/// Suitable for development and tests where audit trail persistence is not required.
/// </summary>
/// <remarks>
/// Production applications must replace this with a durable store via
/// <c>AddGranitWebhooksEntityFrameworkCore()</c> to satisfy HDS audit trail requirements.
/// </remarks>
internal sealed class NullWebhookDeliveryWriter : IWebhookDeliveryWriter
{
    public Task RecordSuccessAsync(SendWebhookCommand command, int httpStatusCode, long durationMs, string payloadHash, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RecordFailureAsync(SendWebhookCommand command, int? httpStatusCode, long durationMs, string errorMessage, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SuspendSubscriptionAsync(Guid subscriptionId, string reason, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
