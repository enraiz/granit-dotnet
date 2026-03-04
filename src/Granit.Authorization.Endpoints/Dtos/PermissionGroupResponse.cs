namespace Granit.Authorization.Endpoints.Dtos;

/// <summary>
/// Response representing a permission group with its child permission definitions.
/// </summary>
public sealed record PermissionGroupResponse(
    string Name,
    string? DisplayName,
    IReadOnlyList<PermissionDefinitionResponse> Permissions);
