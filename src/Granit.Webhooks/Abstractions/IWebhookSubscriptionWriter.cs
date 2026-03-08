namespace Granit.Webhooks.Abstractions;

/// <summary>
/// Write operations for webhook subscriptions (administrative actions).
/// </summary>
public interface IWebhookSubscriptionWriter
{
    /// <summary>
    /// Permanently deactivates a subscription.
    /// </summary>
    /// <param name="subscriptionId">Target subscription identifier.</param>
    /// <param name="reason">Human-readable deactivation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeactivateAsync(Guid subscriptionId, string reason, CancellationToken cancellationToken = default);
}
