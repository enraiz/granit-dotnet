using Granit.Authorization.Abstractions;
using Granit.Core.MultiTenancy;
using Granit.Identity;
using Granit.Identity.Models;
using Microsoft.Extensions.Logging;

namespace Granit.Workflow.Notifications.Internal;

/// <summary>
/// Resolves workflow approvers by combining <see cref="IPermissionManager"/> (permission → roles)
/// with <see cref="IIdentityProvider"/> (roles → users).
/// </summary>
/// <remarks>
/// <para>
/// Resolution flow:
/// <list type="number">
///   <item>
///     <see cref="IPermissionManager.GetGrantedRolesAsync"/> retrieves role names
///     granted the required permission (from the authorization database).
///   </item>
///   <item>
///     For each role, <see cref="IIdentityProvider.GetRoleMembersAsync"/> returns the members.
///   </item>
///   <item>User IDs are aggregated and deduplicated.</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class IdentityApproverResolver(
    IPermissionManager permissionManager,
    IIdentityProvider identityProvider,
    ICurrentTenant currentTenant,
    ILogger<IdentityApproverResolver> logger) : IApproverResolver
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ResolveApproversAsync(
        string requiredPermission,
        CancellationToken cancellationToken = default)
    {
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;

        IReadOnlyList<string> roles = await permissionManager.GetGrantedRolesAsync(
            requiredPermission, tenantId, cancellationToken).ConfigureAwait(false);

        if (roles.Count == 0)
        {
            logger.LogDebug(
                "No roles found for permission {Permission} (tenant: {TenantId})",
                requiredPermission, tenantId);
            return [];
        }

        HashSet<string> userIds = [];

        foreach (string role in roles)
        {
            IReadOnlyList<IdentityUser> members = await identityProvider.GetRoleMembersAsync(
                role, cancellationToken).ConfigureAwait(false);

            foreach (IdentityUser user in members)
            {
                userIds.Add(user.Id);
            }
        }

        logger.LogDebug(
            "Resolved {UserCount} approvers for permission {Permission} across {RoleCount} roles",
            userIds.Count, requiredPermission, roles.Count);

        return [.. userIds];
    }
}
