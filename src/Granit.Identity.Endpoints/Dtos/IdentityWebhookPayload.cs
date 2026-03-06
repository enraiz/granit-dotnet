namespace Granit.Identity.Endpoints.Dtos;

/// <summary>
/// Generic webhook payload from an identity provider.
/// The application host translates provider-specific formats into this structure.
/// </summary>
/// <param name="EventType">Event type: <c>user_updated</c> or <c>user_deleted</c>.</param>
/// <param name="UserId">External user ID in the identity provider.</param>
/// <param name="Timestamp">Event timestamp from the provider.</param>
public sealed record IdentityWebhookPayload(
    string EventType,
    string UserId,
    DateTimeOffset? Timestamp = null);
