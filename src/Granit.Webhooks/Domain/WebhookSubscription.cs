using Granit.Core.Domain;

namespace Granit.Webhooks.Domain;

/// <summary>
/// Represents an external subscriber registered to receive webhook events.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SigningSecret"/> field stores an opaque protected value whose format
/// is determined by the registered <see cref="Abstractions.IWebhookSecretProtector"/>.
/// Never log or expose this value. At delivery time, the handler calls
/// <see cref="Abstractions.IWebhookSecretProtector.UnprotectAsync"/> before signing.
/// </para>
/// <para>
/// ISO 27001 compliance: suspension actions are traced with <see cref="SuspendedAt"/>
/// and <see cref="SuspendedBy"/> (UserId only, never PII).
/// </para>
/// </remarks>
public sealed class WebhookSubscription : AuditedEntity
{
    /// <summary>
    /// The HTTPS endpoint that receives webhook HTTP POST requests.
    /// Maximum length: 2048 characters.
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// Logical event type this subscription is registered for (e.g., <c>"document.uploaded"</c>).
    /// Maximum length: 200 characters.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Protected signing secret used to compute the <c>x-granit-signature</c> HMAC.
    /// The raw value is opaque — protected by <see cref="Abstractions.IWebhookSecretProtector"/>.
    /// Never store or log the plaintext secret. Maximum length: 1000 characters.
    /// </summary>
    public string SigningSecret { get; set; } = string.Empty;

    /// <summary>
    /// Tenant this subscription belongs to.
    /// <c>null</c> indicates a global subscription that applies regardless of tenant context.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>Current lifecycle status of the subscription.</summary>
    public WebhookSubscriptionStatus Status { get; set; } = WebhookSubscriptionStatus.Active;

    /// <summary>
    /// Human-readable reason for suspension or deactivation.
    /// Set automatically on HTTP non-retriable errors. Maximum length: 500 characters.
    /// </summary>
    public string? DeactivationReason { get; set; }

    /// <summary>
    /// Number of consecutive delivery failures since the last successful delivery.
    /// Reset to zero on success.
    /// </summary>
    public int ConsecutiveFailureCount { get; set; }

    /// <summary>UTC timestamp of the last successful delivery. Null if never delivered.</summary>
    public DateTimeOffset? LastSuccessAt { get; set; }

    /// <summary>UTC timestamp when the subscription was suspended. ISO 27001 audit field.</summary>
    public DateTimeOffset? SuspendedAt { get; set; }

    /// <summary>
    /// UserId (not PII) of the operator who suspended the subscription, or the system identifier
    /// for automatic suspensions. ISO 27001 audit field. Maximum length: 450 characters.
    /// </summary>
    public string? SuspendedBy { get; set; }
}
