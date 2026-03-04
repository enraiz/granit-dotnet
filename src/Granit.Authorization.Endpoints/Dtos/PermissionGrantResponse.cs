namespace Granit.Authorization.Endpoints.Dtos;

/// <summary>
/// Response containing the list of permissions explicitly granted to a role.
/// </summary>
public sealed record PermissionGrantResponse(string RoleName, IReadOnlyList<string> Permissions);
