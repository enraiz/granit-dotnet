namespace Granit.Workflow;

/// <summary>
/// Orchestrates workflow transitions with permission checking and approval routing.
/// When a user lacks the required permission on a transition marked with
/// <see cref="WorkflowTransition{TState}.RequiresApproval"/>, the entity is routed
/// to a pending review state and approvers are notified.
/// </summary>
/// <typeparam name="TState">Enum type representing the workflow states.</typeparam>
public interface IWorkflowManager<TState> where TState : struct, Enum
{
    /// <summary>
    /// Returns the transitions available from <paramref name="currentState"/>
    /// for the current user, considering their permissions.
    /// Transitions where the user lacks the required permission but approval routing
    /// is enabled are still included (with <see cref="WorkflowTransition{TState}.RequiresApproval"/> = true).
    /// </summary>
    Task<IReadOnlyList<WorkflowTransition<TState>>> GetAllowedTransitionsAsync(
        TState currentState,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to transition from <paramref name="currentState"/> to <paramref name="targetState"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="TransitionResult{TState}"/> describing the outcome:
    /// <list type="bullet">
    ///   <item><see cref="TransitionOutcome.Completed"/>: transition succeeded directly.</item>
    ///   <item><see cref="TransitionOutcome.ApprovalRequested"/>: routed to pending review, approvers notified.</item>
    ///   <item><see cref="TransitionOutcome.Denied"/>: user lacks permission and no approval path.</item>
    ///   <item><see cref="TransitionOutcome.InvalidTransition"/>: no such transition in the definition.</item>
    /// </list>
    /// </returns>
    Task<TransitionResult<TState>> TransitionAsync(
        TState currentState,
        TState targetState,
        TransitionContext? context = null,
        CancellationToken cancellationToken = default);
}
