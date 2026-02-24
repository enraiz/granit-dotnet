using Granit.Webhooks.Domain;

namespace Granit.Webhooks.Abstractions;

/// <summary>
/// Pluggable store for webhook subscriptions.
/// </summary>
/// <remarks>
/// The default registration is <c>InMemoryWebhookSubscriptionStore</c> (development and tests).
/// Production applications should call <c>AddGranitWebhooksEntityFrameworkCore()</c>
/// from <c>Granit.Webhooks.EntityFrameworkCore</c> to replace it with a durable EF Core store.
/// </remarks>
public interface IWebhookSubscriptionStore
{
    /// <summary>
    /// Returns all active subscriptions matching the given event type and tenant context.
    /// </summary>
    /// <remarks>
    /// Subscriptions with <c>TenantId = null</c> (global) are always included regardless
    /// of the <paramref name="tenantId"/> value.
    /// </remarks>
    /// <param name="eventType">Logical event type (e.g., <c>"document.uploaded"</c>).</param>
    /// <param name="tenantId">Tenant context. <c>null</c> returns only global subscriptions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<WebhookSubscription>> GetActiveSubscriptionsAsync(
        string eventType,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the subscription with the given identifier, or <c>null</c> if not found.</summary>
    Task<WebhookSubscription?> FindByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deactivates a subscription.
    /// </summary>
    /// <param name="subscriptionId">Target subscription identifier.</param>
    /// <param name="reason">Human-readable deactivation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeactivateAsync(Guid subscriptionId, string reason, CancellationToken cancellationToken = default);
}
