namespace Granit.Identity.Endpoints.Dtos;

/// <summary>
/// Request body for batch resolving user IDs to identity information.
/// </summary>
/// <param name="UserIds">External user IDs to resolve.</param>
public sealed record IdentityUserCacheBatchRequest(IReadOnlyList<string> UserIds);
