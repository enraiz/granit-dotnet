using Granit.Core.MultiTenancy;
using Granit.Templating.Store;
using Granit.Workflow;
using Granit.Workflow.Domain;

namespace Granit.Templating.Workflow;

/// <summary>
/// Workflow-aware implementation of <see cref="ITemplateTransitionHook"/>.
/// Delegates transition validation to <see cref="IWorkflowManager{TState}"/> and
/// persists transition records via <see cref="IWorkflowTransitionRecorder"/> for unified HDS audit trail.
/// </summary>
internal sealed class WorkflowTemplateTransitionHook(
    IWorkflowManager<WorkflowLifecycleStatus> workflowManager,
    IWorkflowTransitionRecorder transitionRecorder,
    ICurrentTenant currentTenant) : ITemplateTransitionHook
{
    private const string EntityTypeName = "TemplateRevision";

    /// <inheritdoc/>
    public bool IsWorkflowEnabled => true;

    /// <inheritdoc/>
    public async Task<bool> CanTransitionAsync(
        TemplateLifecycleStatus from,
        TemplateLifecycleStatus target,
        CancellationToken ct = default)
    {
        WorkflowLifecycleStatus wFrom = ToWorkflow(from);
        WorkflowLifecycleStatus wTo = ToWorkflow(target);
        IReadOnlyList<WorkflowTransition<WorkflowLifecycleStatus>> allowed =
            await workflowManager.GetAllowedTransitionsAsync(wFrom, ct).ConfigureAwait(false);
        return allowed.Any(t => t.To == wTo);
    }

    /// <inheritdoc/>
    public async Task OnTransitionedAsync(
        Guid revisionId,
        TemplateLifecycleStatus from,
        TemplateLifecycleStatus target,
        string userId,
        CancellationToken ct = default)
    {
        Guid? tenantId = currentTenant.IsAvailable ? currentTenant.Id : null;

        await transitionRecorder.RecordTransitionAsync(
            EntityTypeName,
            revisionId.ToString(),
            from.ToString(),
            target.ToString(),
            userId,
            WorkflowTransitionContext.Current?.Comment,
            tenantId,
            ct).ConfigureAwait(false);
    }

    private static WorkflowLifecycleStatus ToWorkflow(TemplateLifecycleStatus status) =>
        (WorkflowLifecycleStatus)(int)status;
}
