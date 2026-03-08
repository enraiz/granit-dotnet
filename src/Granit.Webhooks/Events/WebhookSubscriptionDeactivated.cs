using Granit.Core.Events;

namespace Granit.Webhooks.Events;

/// <summary>
/// Raised when a webhook subscription is permanently deactivated.
/// </summary>
public sealed record WebhookSubscriptionDeactivated(
    Guid SubscriptionId,
    string Reason) : IDomainEvent;
