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
    /// Default: <c>"api/workflow"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "api/workflow";

    /// <summary>
    /// OpenAPI tag name for grouping workflow endpoints in Swagger UI.
    /// Default: <c>"Workflow"</c>.
    /// </summary>
    public string TagName { get; set; } = "Workflow";
}
