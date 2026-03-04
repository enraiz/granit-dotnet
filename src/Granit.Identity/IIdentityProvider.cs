using Granit.Identity.Models;

namespace Granit.Identity;

/// <summary>
/// Provides read access to identity users and roles from an external identity provider
/// (Keycloak, LDAP, Entra ID, etc.).
/// </summary>
/// <remarks>
/// A <c>NullIdentityProvider</c> is registered by default (returns empty lists).
/// Install a provider package (e.g. <c>Granit.Identity.Keycloak</c>) to connect
/// to a real identity system.
/// </remarks>
public interface IIdentityProvider
{
    /// <summary>
    /// Lists users, optionally filtered by a search term with pagination.
    /// </summary>
    /// <param name="search">Free-text search (username, email, name). <c>null</c> returns all.</param>
    /// <param name="first">Zero-based index of the first result (pagination offset).</param>
    /// <param name="max">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching users, or an empty list if none found or the provider is unavailable.</returns>
    Task<IReadOnlyList<IdentityUser>> GetUsersAsync(
        string? search = null,
        int? first = null,
        int? max = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single user by their external identity provider ID.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user, or <c>null</c> if not found.</returns>
    Task<IdentityUser?> GetUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all roles defined in the identity provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All roles, or an empty list if the provider is unavailable.</returns>
    Task<IReadOnlyList<IdentityRole>> GetRolesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists users assigned to a given role.
    /// </summary>
    /// <param name="roleName">The role name to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Users holding the role, or an empty list if the role has no members.</returns>
    Task<IReadOnlyList<IdentityUser>> GetRoleMembersAsync(
        string roleName,
        CancellationToken cancellationToken = default);
}
