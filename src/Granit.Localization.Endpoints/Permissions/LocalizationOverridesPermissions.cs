namespace Granit.Localization.Endpoints.Permissions;

/// <summary>
/// Permission constants for the localization override management endpoints.
/// Use these names when granting permissions via <c>IPermissionManager.SetAsync()</c>
/// or when checking access via <c>IPermissionChecker.IsGrantedAsync()</c>.
/// </summary>
public static class LocalizationOverridesPermissions
{
    /// <summary>Permission group name used in <c>IPermissionDefinitionContext.AddGroup()</c>.</summary>
    public const string GroupName = "Localization";

    /// <summary>
    /// Grants access to all localization override management endpoints
    /// (list overrides, set override, remove override).
    /// </summary>
    public const string Manage = "Localization.Overrides.Manage";
}
