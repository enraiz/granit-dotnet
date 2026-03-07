namespace Granit.Identity.Models;

/// <summary>
/// Represents the fields that can be updated on an identity provider user.
/// All properties are optional — only non-null values are applied.
/// </summary>
/// <param name="Email">New email address, or <c>null</c> to leave unchanged.</param>
/// <param name="FirstName">New first name, or <c>null</c> to leave unchanged.</param>
/// <param name="LastName">New last name, or <c>null</c> to leave unchanged.</param>
public sealed record IdentityUserUpdate(
    string? Email = null,
    string? FirstName = null,
    string? LastName = null);
