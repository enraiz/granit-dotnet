using Granit.Webhooks.Domain;

namespace Granit.Webhooks.Abstractions;

/// <summary>
/// Read operations for webhook delivery attempts.
/// </summary>
/// <remarks>
/// The default registration is <c>NullWebhookDeliveryReader</c> (always returns <c>null</c>).
/// Production applications must call <c>AddGranitWebhooksEntityFrameworkCore()</c> to enable
/// durable queries against the ISO 27001 audit trail.
/// </remarks>
public interface IWebhookDeliveryReader
{
    /// <summary>
    /// Returns the delivery attempt with the given <paramref name="deliveryId"/>, or <c>null</c> if not found.
    /// </summary>
    Task<WebhookDeliveryAttempt?> FindByDeliveryIdAsync(Guid deliveryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of delivery attempts that occurred before <paramref name="cutoff"/>.
    /// </summary>
    /// <remarks>
    /// Used by archival background jobs to determine the total number of records eligible
    /// for deletion before starting the batch-delete loop.
    /// </remarks>
    /// <param name="cutoff">Only attempts that occurred before this instant are counted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int> CountBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
