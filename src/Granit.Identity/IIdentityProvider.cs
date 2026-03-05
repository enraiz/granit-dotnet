using Granit.Identity.Models;

namespace Granit.Identity;

/// <summary>
/// Provides read and write access to identity users and roles from an external identity provider
/// (Keycloak, LDAP, Entra ID, etc.).
/// </summary>
/// <remarks>
/// A <c>NullIdentityProvider</c> is registered by default (returns empty lists / no-ops).
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
    /// Enables or disables a user account in the identity provider.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="enabled"><c>true</c> to enable the account; <c>false</c> to disable it.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown if the identity provider returns an error.</exception>
    Task SetUserEnabledAsync(
        string userId,
        bool enabled,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists active SSO sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active sessions, or an empty list if the user has no sessions or the provider is unavailable.</returns>
    Task<IReadOnlyList<IdentitySession>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists device activity for a user, grouped by device.
    /// </summary>
    /// <remarks>
    /// Device-level information (OS, browser, device type) is available only when the provider
    /// supports it and is configured accordingly (e.g. Keycloak with token exchange enabled).
    /// Falls back to session-level data when device details are unavailable.
    /// </remarks>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Device activity entries, or an empty list if the provider is unavailable.</returns>
    Task<IReadOnlyList<IdentityDeviceActivity>> GetUserDeviceActivityAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the date and time when the user last changed their password.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The UTC date of the last password change, or <c>null</c> if the user has no password
    /// credential or the provider is unavailable.
    /// </returns>
    Task<DateTimeOffset?> GetPasswordChangedAtAsync(
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
