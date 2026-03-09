namespace Granit.Authentication.ApiKeys.Endpoints.Dtos;

/// <summary>
/// Request to create a new API key.
/// </summary>
/// <param name="Name">Display name for the API key.</param>
/// <param name="Type">Key type (Secret, Publishable, Webhook, Ephemeral).</param>
/// <param name="Environment">Target environment (<c>live</c>, <c>test</c>, <c>dev</c>).</param>
/// <param name="Permissions">Permissions granted to this key.</param>
/// <param name="AllowedCidrs">Allowed CIDR ranges for IP whitelisting. Empty means no restriction.</param>
/// <param name="ExpiresAt">Optional expiration date.</param>
/// <param name="CacheBehavior">Cache behavior. Default: <see cref="ApiKeys.CacheBehavior.Normal"/>.</param>
public sealed record ApiKeyCreateRequest(
    string Name,
    ApiKeyType Type,
    string Environment,
    List<string>? Permissions = null,
    List<string>? AllowedCidrs = null,
    DateTimeOffset? ExpiresAt = null,
    CacheBehavior CacheBehavior = CacheBehavior.Normal);
