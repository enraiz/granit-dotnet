namespace Granit.Authentication.ApiKeys.Endpoints.Dtos;

/// <summary>
/// Query parameters for listing API keys.
/// </summary>
internal sealed record ApiKeyListRequest(
    string? Search = null,
    ApiKeyType? Type = null,
    string? Environment = null,
    bool IncludeRevoked = false,
    int Page = 1,
    int PageSize = 20);
