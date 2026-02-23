namespace Granit.Authorization.Abstractions;

/// <summary>Defines a single permission that can be granted to a role.</summary>
public sealed record PermissionDefinition(string Name, string? DisplayName, string GroupName);
