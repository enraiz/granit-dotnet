using Granit.Workflow.Domain;

namespace Granit.Workflow.Definitions;

/// <summary>
/// Pre-built workflow definition for the standard publication lifecycle:
/// Draft → PendingReview → Published → Archived.
/// </summary>
/// <remarks>
/// Consumers can use this directly or create their own custom definitions
/// using <see cref="WorkflowDefinition{TState}.Create"/>.
/// </remarks>
public static class PublicationWorkflow
{
    /// <summary>
    /// Standard publication workflow with approval routing.
    /// </summary>
    public static WorkflowDefinition<WorkflowLifecycleStatus> Default { get; } =
        WorkflowDefinition<WorkflowLifecycleStatus>.Create(builder => builder
            .InitialState(WorkflowLifecycleStatus.Draft)
            .Transition(WorkflowLifecycleStatus.Draft, WorkflowLifecycleStatus.PendingReview, t => t
                .Named("Soumettre pour validation")
                .RequiresPermission("workflow.submit"))
            .Transition(WorkflowLifecycleStatus.PendingReview, WorkflowLifecycleStatus.Published, t => t
                .Named("Approuver et publier")
                .RequiresPermission("workflow.publish")
                .RequiresApproval())
            .Transition(WorkflowLifecycleStatus.Draft, WorkflowLifecycleStatus.Published, t => t
                .Named("Publication directe")
                .RequiresPermission("workflow.publish")
                .RequiresApproval())
            .Transition(WorkflowLifecycleStatus.Published, WorkflowLifecycleStatus.Archived, t => t
                .Named("Archiver")
                .RequiresPermission("workflow.archive"))
            .Transition(WorkflowLifecycleStatus.Published, WorkflowLifecycleStatus.Draft, t => t
                .Named("Créer nouvelle version")));
}
