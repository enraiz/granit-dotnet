namespace Granit.Authorization.Endpoints.Permissions;

/// <summary>
/// Permission name constants for the authorization management endpoints.
/// </summary>
public static class AuthorizationEndpointsPermissions
{
    /// <summary>Permission group name.</summary>
    public const string GroupName = "Authorization";

    /// <summary>Permissions for reading permission definitions.</summary>
    public static class Definitions
    {
        /// <summary>View all registered permission definitions and groups.</summary>
        public const string Read = "Authorization.Definitions.Read";
    }

    /// <summary>Permissions for managing role → permission grants.</summary>
    public static class Grants
    {
        /// <summary>View, grant, and revoke permissions for roles.</summary>
        public const string Manage = "Authorization.Grants.Manage";
    }
}
