namespace Granit.Identity.Models;

/// <summary>
/// Represents a role from an external identity provider.
/// </summary>
/// <param name="Id">Unique role identifier in the identity provider.</param>
/// <param name="Name">Role name.</param>
/// <param name="Description">Optional role description.</param>
public sealed record IdentityRole(
    string Id,
    string Name,
    string? Description);
