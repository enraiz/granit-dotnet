namespace Granit.Workflow.Endpoints;

/// <summary>
/// Configuration options for the workflow administration endpoints.
/// </summary>
public sealed class WorkflowEndpointsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "WorkflowEndpoints";

    /// <summary>
    /// Route prefix for all workflow endpoints.
    /// Default: <c>"workflow"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "workflow";

    /// <summary>
    /// Granit permission required to access the workflow endpoints.
    /// Default: <c>"granit-workflow-admin"</c>.
    /// </summary>
    public string RequiredRole { get; set; } = "granit-workflow-admin";

    /// <summary>
    /// OpenAPI tag name for grouping workflow endpoints in Swagger UI.
    /// Default: <c>"Workflow"</c>.
    /// </summary>
    public string TagName { get; set; } = "Workflow";
}
