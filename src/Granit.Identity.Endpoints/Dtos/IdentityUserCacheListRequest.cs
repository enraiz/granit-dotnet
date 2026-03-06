namespace Granit.Identity.Endpoints.Dtos;

/// <summary>
/// Query string parameters for listing cached identity users.
/// </summary>
internal sealed record IdentityUserCacheListRequest(
    string? Search = null,
    int Page = 1,
    int PageSize = 20);
