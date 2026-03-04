namespace Granit.Identity.Models;

/// <summary>
/// Represents a user from an external identity provider.
/// </summary>
/// <param name="Id">Unique user identifier in the identity provider.</param>
/// <param name="Username">Login name.</param>
/// <param name="Email">Email address.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="Enabled">Whether the user account is active.</param>
public sealed record IdentityUser(
    string Id,
    string? Username,
    string? Email,
    string? FirstName,
    string? LastName,
    bool Enabled);
