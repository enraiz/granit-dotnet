namespace Granit.Identity.Endpoints.Dtos;

/// <summary>
/// Request body for syncing specific users from the identity provider.
/// </summary>
/// <param name="UserIds">External user IDs to refresh from the identity provider.</param>
public sealed record IdentityUserCacheSyncRequest(IReadOnlyList<string> UserIds);
