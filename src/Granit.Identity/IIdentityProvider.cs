using Granit.Identity.Models;

namespace Granit.Identity;

/// <summary>
/// Provides read and write access to identity users and roles from an external identity provider
/// (Keycloak, LDAP, Entra ID, etc.).
/// </summary>
/// <remarks>
/// <para>
/// A <c>NullIdentityProvider</c> is registered by default (returns empty lists / no-ops).
/// Install a provider package (e.g. <c>Granit.Identity.Keycloak</c>) to connect
/// to a real identity system.
/// </para>
/// <para>
/// Write operations (<see cref="SetUserEnabledAsync"/>, <see cref="UpdateUserAsync"/>)
/// propagate exceptions on failure. For Keycloak, the service account must hold the
/// <c>realm-management:manage-users</c> role to perform write operations.
/// </para>
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
    /// Updates a user's profile in the identity provider.
    /// Only non-null fields in <paramref name="update"/> are applied.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="update">The fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown if the identity provider returns an error.</exception>
    Task UpdateUserAsync(
        string userId,
        IdentityUserUpdate update,
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

    // ──── Feature 1: User role management ────

    /// <summary>
    /// Lists realm-level roles assigned to a user.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Roles assigned to the user, or an empty list if the provider is unavailable.</returns>
    Task<IReadOnlyList<IdentityRole>> GetUserRolesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a realm-level role to a user.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="roleName">The role name to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown if the identity provider returns an error.</exception>
    Task AssignRoleAsync(
        string userId,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a realm-level role from a user.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="roleName">The role name to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown if the identity provider returns an error.</exception>
    Task RemoveRoleAsync(
        string userId,
        string roleName,
        CancellationToken cancellationToken = default);

    // ──── Feature 2: Session termination ────

    /// <summary>
    /// Terminates a specific SSO session.
    /// </summary>
    /// <param name="userId">The user ID (used for logging/validation).</param>
    /// <param name="sessionId">The session ID to terminate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown if the identity provider returns an error.</exception>
    Task TerminateSessionAsync(
        string userId,
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminates all active SSO sessions for a user (logs the user out everywhere).
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown if the identity provider returns an error.</exception>
    Task TerminateAllSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    // ──── Feature 3: Password reset ────

    /// <summary>
    /// Sends a password reset email to the user via the identity provider.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown if the identity provider returns an error.</exception>
    Task SendPasswordResetEmailAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a temporary password for the user. The user will be required to change it at next login.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="temporaryPassword">The temporary password to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown if the identity provider returns an error.</exception>
    Task SetTemporaryPasswordAsync(
        string userId,
        string temporaryPassword,
        CancellationToken cancellationToken = default);

    // ──── Feature 4: User creation ────

    /// <summary>
    /// Creates a new user in the identity provider.
    /// </summary>
    /// <param name="user">The user data for creation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user with its provider-assigned ID.</returns>
    /// <exception cref="HttpRequestException">Thrown if the identity provider returns an error.</exception>
    Task<IdentityUser> CreateUserAsync(
        IdentityUserCreate user,
        CancellationToken cancellationToken = default);

    // ──── Feature 5: Group management ────

    /// <summary>
    /// Lists all groups defined in the identity provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All groups, or an empty list if the provider is unavailable.</returns>
    Task<IReadOnlyList<IdentityGroup>> GetGroupsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists groups that a user belongs to.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Groups the user is a member of, or an empty list if the provider is unavailable.</returns>
    Task<IReadOnlyList<IdentityGroup>> GetUserGroupsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user to a group.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="groupId">The group ID to add the user to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown if the identity provider returns an error.</exception>
    Task AddUserToGroupAsync(
        string userId,
        string groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user from a group.
    /// </summary>
    /// <param name="userId">The user ID in the identity provider.</param>
    /// <param name="groupId">The group ID to remove the user from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown if the identity provider returns an error.</exception>
    Task RemoveUserFromGroupAsync(
        string userId,
        string groupId,
        CancellationToken cancellationToken = default);
}
