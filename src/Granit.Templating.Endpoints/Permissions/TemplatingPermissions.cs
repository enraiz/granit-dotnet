namespace Granit.Templating.Endpoints.Permissions;

/// <summary>
/// Permission constants for the template administration endpoints.
/// </summary>
public static class TemplatingPermissions
{
    /// <summary>Permission group name used in <c>IPermissionDefinitionContext.AddGroup()</c>.</summary>
    public const string GroupName = "Templating";

    /// <summary>
    /// Grants access to all template administration endpoints
    /// (list, detail, save draft, delete draft, publish, unpublish, history).
    /// </summary>
    public const string Manage = "Templates.Manage";
}
