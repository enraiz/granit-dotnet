using System.Text.Json;

namespace Granit.Webhooks.Messages;

/// <summary>
/// The standardized JSON envelope sent to subscriber endpoints via HTTP POST.
/// </summary>
/// <remarks>
/// This record is serialized directly as the HTTP request body.
/// Its structure is stable and versioned via <see cref="ApiVersion"/> — consumers
/// should not break when new fields are added.
/// </remarks>
public sealed record WebhookEnvelope
{
    /// <summary>
    /// Unique identifier of the logical event.
    /// Identical across all delivery attempts for the same event.
    /// Clients can use this to deduplicate.
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>Logical event type (e.g., <c>"document.uploaded"</c>).</summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Tenant that generated this event.
    /// <c>null</c> when multi-tenancy is not active.
    /// </summary>
    public required Guid? TenantId { get; init; }

    /// <summary>UTC timestamp when the event occurred on the server.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Webhook contract version. Clients should check this value to handle
    /// breaking changes in future payload structures.
    /// </summary>
    public required string ApiVersion { get; init; }

    /// <summary>
    /// Event payload. Either a thin payload (ResourceId + fetch URL, for ISO 27001 compliance)
    /// or a fat payload (full DTO). The structure depends on the event type and the
    /// application configuration.
    /// </summary>
    public required JsonElement Data { get; init; }
}
