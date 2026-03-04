namespace Granit.Authorization.Endpoints.Dtos;

/// <summary>
/// Response containing the list of permission names granted to the current user.
/// </summary>
public sealed record MyPermissionsResponse(IReadOnlyList<string> Permissions);
