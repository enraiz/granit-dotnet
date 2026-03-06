namespace Granit.Identity.Endpoints.Options;

/// <summary>
/// Configuration options for the identity webhook endpoint.
/// Bind to the <c>IdentityWebhook</c> configuration section.
/// </summary>
public sealed class IdentityWebhookOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "IdentityWebhook";

    /// <summary>
    /// Shared secret for HMAC-SHA256 signature validation.
    /// The provider must send the signature in the <c>X-Webhook-Signature</c> header.
    /// Leave empty to disable signature validation (not recommended for production).
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Name of the HTTP header containing the HMAC signature.
    /// Default: <c>"X-Webhook-Signature"</c>.
    /// </summary>
    public string SignatureHeaderName { get; set; } = "X-Webhook-Signature";
}
