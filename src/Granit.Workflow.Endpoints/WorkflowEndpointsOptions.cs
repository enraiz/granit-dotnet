namespace Granit.Workflow.Endpoints;

/// <summary>
/// Configuration options for the workflow administration endpoints.
/// </summary>
public sealed class WorkflowEndpointsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "WorkflowEndpoints";

    /// <summary>
    /// Optional API prefix prepended to <see cref="RoutePrefix"/>
    /// (e.g., <c>"api/v1"</c>). Empty by default (no prefix).
    /// </summary>
    public string ApiPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Route prefix for all workflow endpoints.
    /// Default: <c>"workflow"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "workflow";

    /// <summary>
    /// OpenAPI tag name for grouping workflow endpoints in Swagger UI.
    /// Default: <c>"Workflow"</c>.
    /// </summary>
    public string TagName { get; set; } = "Workflow";
}
