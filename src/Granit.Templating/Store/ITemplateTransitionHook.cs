namespace Granit.Templating.Store;

/// <summary>
/// Extension point for template lifecycle transitions.
/// </summary>
/// <remarks>
/// <para>
/// The default implementation (<see cref="NullTemplateTransitionHook"/>) allows simple
/// <c>Draft → Published → Archived</c> transitions without any external dependency.
/// </para>
/// <para>
/// Install the <c>Granit.Templating.Workflow</c> bridge package to replace this with a
/// Workflow-aware implementation that provides FSM validation, approval routing,
/// unified HDS audit trail and domain events.
/// </para>
/// </remarks>
public interface ITemplateTransitionHook
{
    /// <summary>
    /// Indicates whether the Workflow module is providing the hook implementation.
    /// </summary>
    bool IsWorkflowEnabled { get; }

    /// <summary>
    /// Validates whether a lifecycle transition is allowed.
    /// </summary>
    /// <param name="from">Current lifecycle status.</param>
    /// <param name="target">Target lifecycle status.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the transition is allowed; otherwise <c>false</c>.</returns>
    Task<bool> CanTransitionAsync(
        TemplateLifecycleStatus from,
        TemplateLifecycleStatus target,
        CancellationToken ct = default);

    /// <summary>
    /// Invoked after a lifecycle transition has been persisted.
    /// </summary>
    /// <param name="revisionId">Unique identifier of the revision that transitioned.</param>
    /// <param name="from">Previous lifecycle status.</param>
    /// <param name="target">New lifecycle status.</param>
    /// <param name="userId">Identity of the user who triggered the transition.</param>
    /// <param name="ct">Cancellation token.</param>
    Task OnTransitionedAsync(
        Guid revisionId,
        TemplateLifecycleStatus from,
        TemplateLifecycleStatus target,
        string userId,
        CancellationToken ct = default);
}
