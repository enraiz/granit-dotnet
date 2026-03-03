namespace Granit.DataExchange.Endpoints.Permissions;

/// <summary>
/// Permission constants for the <c>Granit.DataExchange.Endpoints</c> module.
/// Use these names when granting permissions via <c>IPermissionManager.SetAsync()</c>
/// or when checking access via <c>IPermissionChecker.IsGrantedAsync()</c>.
/// </summary>
public static class DataExchangePermissions
{
    /// <summary>Permission group name used in <c>IPermissionDefinitionContext.AddGroup()</c>.</summary>
    public const string GroupName = "DataExchange";

    /// <summary>Administration permissions for the data import endpoints.</summary>
    public static class Admin
    {
        /// <summary>
        /// Grants access to all data import administration endpoints
        /// (upload, preview, mappings, execute, dry-run, status, report, correction file).
        /// </summary>
        public const string Default = "DataExchange.Import";
    }

    /// <summary>Permissions for the data export endpoints.</summary>
    public static class Export
    {
        /// <summary>
        /// Grants access to all data export endpoints
        /// (definitions, field listing, export execution, download, presets).
        /// </summary>
        public const string Default = "DataExchange.Export";
    }
}
