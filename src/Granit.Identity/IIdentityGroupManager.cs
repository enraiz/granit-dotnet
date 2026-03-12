using Granit.Identity.Models;

namespace Granit.Identity;

/// <summary>
/// Group management operations: list groups, query membership, add and remove users.
/// </summary>
public interface IIdentityGroupManager
{
    /// <inheritdoc cref="IIdentityProvider.GetGroupsAsync"/>
    Task<IReadOnlyList<IdentityGroup>> GetGroupsAsync(
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IIdentityProvider.GetUserGroupsAsync"/>
    Task<IReadOnlyList<IdentityGroup>> GetUserGroupsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IIdentityProvider.AddUserToGroupAsync"/>
    Task AddUserToGroupAsync(
        string userId,
        string groupId,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IIdentityProvider.RemoveUserFromGroupAsync"/>
    Task RemoveUserFromGroupAsync(
        string userId,
        string groupId,
        CancellationToken cancellationToken = default);
}
