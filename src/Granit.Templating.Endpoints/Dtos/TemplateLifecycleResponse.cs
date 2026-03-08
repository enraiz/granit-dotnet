using Granit.Templating.Store;

namespace Granit.Templating.Endpoints.Dtos;

/// <summary>
/// Response DTO for the lifecycle status endpoint.
/// </summary>
/// <param name="Name">Logical template name.</param>
/// <param name="Culture">BCP 47 culture tag, or <c>null</c> for culture-neutral templates.</param>
/// <param name="CurrentStatus">Current lifecycle status of the template.</param>
/// <param name="WorkflowEnabled">Whether the Workflow module provides the transition hook.</param>
/// <param name="AvailableTransitions">List of valid target statuses from the current status.</param>
public sealed record TemplateLifecycleResponse(
    string Name,
    string? Culture,
    TemplateLifecycleStatus CurrentStatus,
    bool WorkflowEnabled,
    IReadOnlyList<TemplateLifecycleStatus> AvailableTransitions);
