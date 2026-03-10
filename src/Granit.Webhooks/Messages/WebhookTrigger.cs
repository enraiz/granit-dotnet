using System.Text.Json;

namespace Granit.Webhooks.Messages;

/// <summary>
/// Wolverine message published by the application to trigger webhook fan-out.
/// </summary>
/// <remarks>
/// <para>
/// This is the entry point into the webhook engine. The application publishes this message
/// after its business logic completes. The <see cref="Handlers.WebhookFanoutHandler"/> then
/// resolves active subscribers and produces one <see cref="SendWebhookCommand"/> per subscriber,
/// all within the same Outbox transaction.
/// </para>
/// <para>
/// The <see cref="Payload"/> is opaque to the engine — it can hold a thin payload
/// (ResourceId + fetch URL, for HDS) or a fat payload (a full DTO) serialized by
/// <see cref="Abstractions.IWebhookPublisher"/>. The engine signs and forwards it as-is.
/// </para>
/// </remarks>
public sealed record WebhookTrigger
{
    /// <summary>
    /// Unique identifier of this webhook event.
    /// Shared across all delivery attempts for the same logical event.
    /// </summary>
#pragma warning disable GRSEC002 // Wolverine message — default value needed for deserialization
    public Guid EventId { get; init; } = Guid.NewGuid();
#pragma warning restore GRSEC002

    /// <summary>
    /// Logical event type (e.g., <c>"document.uploaded"</c>).
    /// Used to resolve active subscriptions from <see cref="Abstractions.IWebhookSubscriptionReader"/>.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Serialized payload. Can be a thin payload (ResourceId + fetch URL) or a fat payload (full DTO).
    /// The engine does not inspect the content — it is signed and forwarded verbatim.
    /// </summary>
    public required JsonElement Payload { get; init; }

    /// <summary>
    /// Tenant context at the time the event occurred.
    /// <c>null</c> when multi-tenancy is not active.
    /// Takes precedence over the ambient <c>ICurrentTenant</c> in the fan-out handler
    /// when the ambient context is not available.
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>UTC timestamp when the event occurred.</summary>
    public required DateTimeOffset OccurredAt { get; init; }
}
