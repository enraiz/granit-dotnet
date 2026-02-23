using Microsoft.AspNetCore.Authorization;

namespace Granit.Authorization.Authorization;

/// <summary>Authorization requirement carrying the name of the permission to verify.</summary>
public sealed class PermissionRequirement(string permissionName) : IAuthorizationRequirement
{
    /// <summary>The permission name to check, e.g. "Invoices.Delete".</summary>
    public string PermissionName { get; } = permissionName;
}
