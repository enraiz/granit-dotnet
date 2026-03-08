using Granit.Core.Events;

namespace Granit.Webhooks.Events;

/// <summary>
/// Raised when a webhook subscription is suspended after a non-retriable HTTP error.
/// </summary>
public sealed record WebhookSubscriptionSuspended(
    Guid SubscriptionId,
    string Reason) : IDomainEvent;
