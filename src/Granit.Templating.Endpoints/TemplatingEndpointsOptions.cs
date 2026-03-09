namespace Granit.Templating.Endpoints;

/// <summary>
/// Configuration options for the Granit templating admin HTTP endpoints.
/// </summary>
public sealed class TemplatingEndpointsOptions
{
    /// <summary>
    /// Route prefix for all template admin endpoints.
    /// Default: <c>"templates"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "templates";

    /// <summary>
    /// OpenAPI tag name for all template admin endpoints.
    /// Default: <c>"Templates"</c>.
    /// </summary>
    public string TagName { get; set; } = "Templates";
}
