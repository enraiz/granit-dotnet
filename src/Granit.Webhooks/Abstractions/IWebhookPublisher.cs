namespace Granit.Webhooks.Abstractions;

/// <summary>
/// Application-facing façade for publishing webhook triggers into the dispatch engine.
/// </summary>
/// <remarks>
/// Inject this service in application handlers. It serializes the payload, injects
/// the ambient tenant context (when available), and publishes a
/// <see cref="Messages.WebhookTrigger"/> into the Wolverine Outbox.
/// </remarks>
public interface IWebhookPublisher
{
    /// <summary>
    /// Publishes a webhook trigger for the given event type and payload.
    /// The payload is serialized immediately; the ambient <c>ICurrentTenant</c> is captured
    /// if available (<c>IsAvailable = true</c>).
    /// </summary>
    /// <typeparam name="TPayload">The payload type. Must be JSON-serializable.</typeparam>
    /// <param name="eventType">Logical event type (e.g., <c>"document.uploaded"</c>).</param>
    /// <param name="payload">The event payload — thin or fat, depending on the application.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask PublishAsync<TPayload>(
        string eventType,
        TPayload payload,
        CancellationToken cancellationToken = default) where TPayload : notnull;
}
