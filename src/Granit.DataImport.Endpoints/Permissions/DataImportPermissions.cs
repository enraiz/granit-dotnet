namespace Granit.DataImport.Endpoints.Permissions;

/// <summary>
/// Permission constants for the <c>Granit.DataImport.Endpoints</c> module.
/// Use these names when granting permissions via <c>IPermissionManager.SetAsync()</c>
/// or when checking access via <c>IPermissionChecker.IsGrantedAsync()</c>.
/// </summary>
public static class DataImportPermissions
{
    /// <summary>Permission group name used in <c>IPermissionDefinitionContext.AddGroup()</c>.</summary>
    public const string GroupName = "DataImport";

    /// <summary>Administration permissions for the data import endpoints.</summary>
    public static class Admin
    {
        /// <summary>
        /// Grants access to all data import administration endpoints
        /// (upload, preview, mappings, execute, dry-run, status, report, correction file).
        /// </summary>
        public const string Default = "DataImport.Admin";
    }
}
