using System.Collections.Concurrent;
using Granit.Webhooks.Abstractions;
using Granit.Webhooks.Domain;

namespace Granit.Webhooks.Internal;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IWebhookSubscriptionStore"/>.
/// Suitable for development and unit tests. Does not persist across application restarts.
/// </summary>
internal sealed class InMemoryWebhookSubscriptionStore : IWebhookSubscriptionStore
{
    private readonly ConcurrentDictionary<Guid, WebhookSubscription> _subscriptions = new();

    public Task<IReadOnlyList<WebhookSubscription>> GetActiveSubscriptionsAsync(
        string eventType,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WebhookSubscription> results = _subscriptions.Values
            .Where(s => s.Status == WebhookSubscriptionStatus.Active
                     && s.EventType == eventType
                     && (s.TenantId == null || s.TenantId == tenantId))
            .ToList();

        return Task.FromResult(results);
    }

    public Task<WebhookSubscription?> FindByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        _subscriptions.TryGetValue(subscriptionId, out WebhookSubscription? subscription);
        return Task.FromResult(subscription);
    }

    public Task DeactivateAsync(Guid subscriptionId, string reason, CancellationToken cancellationToken = default)
    {
        if (_subscriptions.TryGetValue(subscriptionId, out WebhookSubscription? subscription))
        {
            subscription.Status = WebhookSubscriptionStatus.Deactivated;
            subscription.DeactivationReason = reason;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds or replaces a subscription. Used in tests and dev scenarios.
    /// </summary>
    internal void Add(WebhookSubscription subscription) =>
        _subscriptions[subscription.Id] = subscription;

    /// <summary>
    /// Suspends a subscription by setting its status and recording the reason.
    /// </summary>
    internal Task SuspendAsync(Guid subscriptionId, string reason, CancellationToken cancellationToken = default)
    {
        if (_subscriptions.TryGetValue(subscriptionId, out WebhookSubscription? subscription))
        {
            subscription.Status = WebhookSubscriptionStatus.Suspended;
            subscription.DeactivationReason = reason;
            subscription.SuspendedAt = DateTimeOffset.UtcNow;
            subscription.SuspendedBy = "system";
        }

        return Task.CompletedTask;
    }
}
