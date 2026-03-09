namespace Granit.Authentication.ApiKeys.Endpoints.Dtos;

/// <summary>
/// Request to update the permissions and CIDR scopes of an API key.
/// </summary>
/// <param name="Permissions">New list of permissions.</param>
/// <param name="AllowedCidrs">New list of allowed CIDR ranges.</param>
public sealed record ApiKeyUpdateScopesRequest(
    List<string> Permissions,
    List<string> AllowedCidrs);
