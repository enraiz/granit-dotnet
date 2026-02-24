namespace Granit.BackgroundJobs.Endpoints.Permissions;

/// <summary>
/// Permission constants for the <c>Granit.BackgroundJobs.Endpoints</c> module.
/// Use these names when granting permissions via <c>IPermissionManager.SetAsync()</c>
/// or when checking access via <c>IPermissionChecker.IsGrantedAsync()</c>.
/// </summary>
public static class BackgroundJobsPermissions
{
    /// <summary>Permission group name used in <c>IPermissionDefinitionContext.AddGroup()</c>.</summary>
    public const string GroupName = "BackgroundJobs";

    /// <summary>Administration permissions for the background jobs endpoints.</summary>
    public static class Admin
    {
        /// <summary>
        /// Grants access to all background jobs administration endpoints
        /// (list, detail, pause, resume, trigger).
        /// </summary>
        public const string Default = "BackgroundJobs.Admin";
    }
}
