namespace Granit.Workflow;

/// <summary>
/// Optional context passed to <c>IWorkflowManager{TState}.TransitionAsync</c>
/// to provide metadata for the transition (e.g. a regulatory comment).
/// </summary>
public sealed record TransitionContext
{
    /// <summary>
    /// Optional comment or regulatory justification for the transition.
    /// Stored in the <c>WorkflowTransitionRecord.Comment</c> field
    /// of the ISO 27001 audit trail via <c>WorkflowTransitionContext</c>.
    /// </summary>
    public string? Comment { get; init; }
}
