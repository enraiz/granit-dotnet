using Granit.Core.MultiTenancy;
using Granit.Guids;
using Granit.Templating.Store;
using Granit.Timing;
using Granit.Workflow;
using Granit.Workflow.Domain;
using Granit.Workflow.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Granit.Templating.Workflow;

/// <summary>
/// Workflow-aware implementation of <see cref="ITemplateTransitionHook"/>.
/// Delegates transition validation to <see cref="IWorkflowManager{TState}"/> and
/// persists <see cref="WorkflowTransitionRecord"/> entries for unified HDS audit trail.
/// </summary>
/// <typeparam name="TDbContext">
/// The host application's <see cref="DbContext"/> implementing <see cref="IWorkflowDbContext"/>.
/// </typeparam>
internal sealed class WorkflowTemplateTransitionHook<TDbContext>(
    IWorkflowManager<WorkflowLifecycleStatus> workflowManager,
    IDbContextFactory<TDbContext> contextFactory,
    IClock clock,
    IGuidGenerator guidGenerator,
    ICurrentTenant currentTenant) : ITemplateTransitionHook
    where TDbContext : DbContext, IWorkflowDbContext
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
        await using TDbContext ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        ctx.WorkflowTransitionRecords.Add(new WorkflowTransitionRecord
        {
            Id = guidGenerator.Create(),
            EntityType = EntityTypeName,
            EntityId = revisionId.ToString(),
            PreviousState = from.ToString(),
            NewState = target.ToString(),
            TransitionedAt = clock.Now,
            TransitionedBy = userId,
            Comment = WorkflowTransitionContext.Current?.Comment,
            TenantId = currentTenant.IsAvailable ? currentTenant.Id : null,
        });
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static WorkflowLifecycleStatus ToWorkflow(TemplateLifecycleStatus status) =>
        (WorkflowLifecycleStatus)(int)status;
}
