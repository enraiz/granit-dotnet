namespace Granit.Settings.Endpoints.Options;

/// <summary>
/// Options for settings endpoints.
/// </summary>
public sealed class SettingsEndpointsOptions
{
    /// <summary>
    /// Route prefix for user-scoped setting endpoints.
    /// Default: <c>"settings/user"</c>.
    /// </summary>
    public string UserRoutePrefix { get; set; } = "settings/user";

    /// <summary>
    /// Route prefix for global setting endpoints.
    /// Default: <c>"settings/global"</c>.
    /// </summary>
    public string GlobalRoutePrefix { get; set; } = "settings/global";

    /// <summary>
    /// Route prefix for tenant-scoped setting endpoints.
    /// Default: <c>"settings/tenant"</c>.
    /// </summary>
    public string TenantRoutePrefix { get; set; } = "settings/tenant";

    /// <summary>
    /// OpenAPI tag name for all settings endpoints.
    /// Default: <c>"Settings"</c>.
    /// </summary>
    public string TagName { get; set; } = "Settings";
}
