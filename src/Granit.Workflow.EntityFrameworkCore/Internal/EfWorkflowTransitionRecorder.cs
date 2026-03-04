using Granit.Guids;
using Granit.Timing;
using Granit.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Workflow.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="IWorkflowTransitionRecorder"/>.
/// Persists <see cref="WorkflowTransitionRecord"/> entries for unified HDS audit trail.
/// </summary>
internal sealed class EfWorkflowTransitionRecorder<TDbContext>(
    IDbContextFactory<TDbContext> contextFactory,
    IClock clock,
    IGuidGenerator guidGenerator) : IWorkflowTransitionRecorder
    where TDbContext : DbContext, IWorkflowDbContext
{
    /// <inheritdoc/>
    public async Task RecordTransitionAsync(
        string entityType,
        string entityId,
        string previousState,
        string newState,
        string userId,
        string? comment,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        await using TDbContext ctx = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        ctx.WorkflowTransitionRecords.Add(new WorkflowTransitionRecord
        {
            Id = guidGenerator.Create(),
            EntityType = entityType,
            EntityId = entityId,
            PreviousState = previousState,
            NewState = newState,
            TransitionedAt = clock.Now,
            TransitionedBy = userId,
            Comment = comment,
            TenantId = tenantId,
        });
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
