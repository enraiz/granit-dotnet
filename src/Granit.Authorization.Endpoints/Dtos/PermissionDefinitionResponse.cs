namespace Granit.Authorization.Endpoints.Dtos;

/// <summary>
/// Response representing a single permission definition.
/// </summary>
public sealed record PermissionDefinitionResponse(string Name, string? DisplayName);
