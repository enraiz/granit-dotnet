namespace Granit.Workflow.Endpoints.Dtos;

/// <summary>
/// Response payload representing the current workflow status of an entity,
/// including the current state and all available transitions.
/// </summary>
/// <param name="CurrentState">Current workflow state name.</param>
/// <param name="AvailableTransitions">Transitions available to the current user.</param>
public sealed record WorkflowStatusResponse(
    string CurrentState,
    IReadOnlyList<WorkflowTransitionResponse> AvailableTransitions);
