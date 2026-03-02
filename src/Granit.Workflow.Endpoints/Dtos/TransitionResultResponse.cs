namespace Granit.Workflow.Endpoints.Dtos;

/// <summary>
/// Response payload for a workflow transition attempt.
/// </summary>
/// <param name="Succeeded">Whether the transition was accepted.</param>
/// <param name="ResultingState">
/// The actual resulting state. May differ from the requested target state
/// when approval routing redirects to "PendingReview".
/// </param>
/// <param name="Outcome">
/// Transition outcome: "Completed", "ApprovalRequested", "Denied", or "InvalidTransition".
/// </param>
public sealed record TransitionResultResponse(
    bool Succeeded,
    string ResultingState,
    string Outcome);
