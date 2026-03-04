namespace Granit.Workflow.Dtos;

/// <summary>
/// Response payload for a single workflow transition history entry (HDS audit trail).
/// </summary>
/// <param name="PreviousState">State before the transition.</param>
/// <param name="NewState">State after the transition.</param>
/// <param name="TransitionedAt">UTC timestamp of the transition.</param>
/// <param name="TransitionedBy">User ID who triggered the transition.</param>
/// <param name="Comment">Optional regulatory comment or justification.</param>
public sealed record TransitionHistoryResponse(
    string PreviousState,
    string NewState,
    DateTimeOffset TransitionedAt,
    string TransitionedBy,
    string? Comment);
