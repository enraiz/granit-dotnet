using Granit.Authorization.Abstractions;

namespace Granit.Authorization.Endpoints.Permissions;

/// <summary>
/// Declares permission definitions used to protect the authorization management endpoints.
/// </summary>
internal sealed class AuthorizationEndpointsPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    /// <inheritdoc />
    public void DefinePermissions(IPermissionDefinitionContext context)
    {
        PermissionGroup group = context.AddGroup(
            AuthorizationEndpointsPermissions.GroupName,
            "Authorization Management");

        group.AddPermission(
            AuthorizationEndpointsPermissions.Definitions.Read,
            "View all registered permission definitions and groups");

        group.AddPermission(
            AuthorizationEndpointsPermissions.Grants.Manage,
            "View, grant, and revoke permissions for roles");
    }
}
