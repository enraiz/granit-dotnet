namespace Granit.Templating.Endpoints;

/// <summary>
/// Configuration options for the Granit templating admin HTTP endpoints.
/// </summary>
public sealed class TemplatingEndpointsOptions
{
    /// <summary>
    /// Optional API prefix prepended to <see cref="RoutePrefix"/>
    /// (e.g., <c>"api/v1"</c>). Default: <c>"api/v1"</c>.
    /// </summary>
    public string ApiPrefix { get; set; } = "api/v1";

    /// <summary>
    /// Route prefix for all template admin endpoints.
    /// Default: <c>"admin/templates"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "admin/templates";

    /// <summary>
    /// OpenAPI tag name for all template admin endpoints.
    /// Default: <c>"Templates Admin"</c>.
    /// </summary>
    public string TagName { get; set; } = "Templates Admin";
}
