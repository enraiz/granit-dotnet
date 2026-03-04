using Granit.Authorization.Endpoints.Permissions;

namespace Granit.Authorization.Endpoints.Internal;

/// <summary>
/// Authorization policy constants for the authorization management endpoints.
/// </summary>
internal static class AuthorizationEndpointsPolicy
{
    /// <summary>Policy name for permission definition read access.</summary>
    internal const string DefinitionsRead = AuthorizationEndpointsPermissions.Definitions.Read;

    /// <summary>Policy name for permission grant management.</summary>
    internal const string GrantsManage = AuthorizationEndpointsPermissions.Grants.Manage;
}
