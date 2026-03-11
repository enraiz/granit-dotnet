namespace Granit.Settings.Endpoints.Permissions;

/// <summary>
/// Permission constants for the settings administration endpoints.
/// </summary>
public static class SettingsPermissions
{
    /// <summary>Permission group name used in <c>IPermissionDefinitionContext.AddGroup()</c>.</summary>
    public const string GroupName = "Settings";

    /// <summary>
    /// Grants read access to global settings.
    /// </summary>
    public const string GlobalRead = "Settings.Global.Read";

    /// <summary>
    /// Grants write access to global settings.
    /// </summary>
    public const string GlobalManage = "Settings.Global.Manage";

    /// <summary>
    /// Grants read access to tenant settings.
    /// </summary>
    public const string TenantRead = "Settings.Tenant.Read";

    /// <summary>
    /// Grants write access to tenant settings.
    /// </summary>
    public const string TenantManage = "Settings.Tenant.Manage";
}
