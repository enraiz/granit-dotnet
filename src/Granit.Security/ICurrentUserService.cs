namespace Granit.Security;

/// <summary>
/// Service for accessing information about the current user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Unique identifier of the user (claim "sub").</summary>
    string? UserId { get; }

    /// <summary>Username (according to the <c>NameClaimType</c> configured in JWT Bearer).</summary>
    string? UserName { get; }

    /// <summary>Email address of the user.</summary>
    string? Email { get; }

    /// <summary>First name of the user (claim "given_name").</summary>
    string? FirstName { get; }

    /// <summary>Last name of the user (claim "family_name").</summary>
    string? LastName { get; }

    /// <summary>Indicates whether the user is authenticated.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Roles assigned to the user.</summary>
    IReadOnlyList<string> GetRoles();

    /// <summary>Checks whether the user has a given role.</summary>
    bool IsInRole(string role);
}
