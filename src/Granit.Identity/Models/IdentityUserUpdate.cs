namespace Granit.Identity.Models;

/// <summary>
/// Represents the fields that can be updated on an identity provider user.
/// All properties are optional — only non-null values are applied.
/// </summary>
/// <param name="Email">New email address, or <c>null</c> to leave unchanged.</param>
/// <param name="FirstName">New first name, or <c>null</c> to leave unchanged.</param>
/// <param name="LastName">New last name, or <c>null</c> to leave unchanged.</param>
/// <param name="Attributes">
/// Custom attributes to set or remove. A <c>null</c> value removes the attribute.
/// Only provided attributes are modified — existing attributes not in the dictionary are left unchanged.
/// </param>
public sealed record IdentityUserUpdate(
    string? Email = null,
    string? FirstName = null,
    string? LastName = null,
    IReadOnlyDictionary<string, string?>? Attributes = null);
