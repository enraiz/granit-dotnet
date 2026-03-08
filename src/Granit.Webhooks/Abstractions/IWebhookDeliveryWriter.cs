using Granit.Webhooks.Messages;

namespace Granit.Webhooks.Abstractions;

/// <summary>
/// Stores webhook delivery attempt records for the HDS audit trail.
/// </summary>
/// <remarks>
/// HDS requirement: every delivery attempt must be recorded and retained for 3 years.
/// The default registration is <c>NullWebhookDeliveryWriter</c> (no-op, for development and tests).
/// Production applications must call <c>AddGranitWebhooksEntityFrameworkCore()</c> to enable
/// durable persistence.
/// </remarks>
public interface IWebhookDeliveryWriter
{
    /// <summary>
    /// Records a successful (2xx) delivery attempt.
    /// Also resets the subscription's consecutive failure counter and updates <c>LastSuccessAt</c>.
    /// </summary>
    Task RecordSuccessAsync(
        SendWebhookCommand command,
        int httpStatusCode,
        long durationMs,
        string payloadHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed delivery attempt and increments the subscription's consecutive failure counter.
    /// </summary>
    Task RecordFailureAsync(
        SendWebhookCommand command,
        int? httpStatusCode,
        long durationMs,
        string errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspends a subscription after a non-retriable HTTP error.
    /// Sets <c>Status = Suspended</c>, records <c>SuspendedAt</c> and the <paramref name="reason"/>.
    /// </summary>
    Task SuspendSubscriptionAsync(
        Guid subscriptionId,
        string reason,
        CancellationToken cancellationToken = default);
}
