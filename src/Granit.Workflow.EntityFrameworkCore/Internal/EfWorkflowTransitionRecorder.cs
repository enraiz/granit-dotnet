using Granit.Guids;
using Granit.Timing;
using Granit.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Workflow.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="IWorkflowTransitionRecorder"/>.
/// Persists <see cref="WorkflowTransitionRecord"/> entries for unified ISO 27001 audit trail.
/// </summary>
internal sealed class EfWorkflowTransitionRecorder<TDbContext>(
    IDbContextFactory<TDbContext> contextFactory,
    IClock clock,
    IGuidGenerator guidGenerator) : IWorkflowTransitionRecorder
    where TDbContext : DbContext, IWorkflowDbContext
{
    /// <inheritdoc/>
    public async Task RecordTransitionAsync(
        RecordTransitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using TDbContext ctx = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        ctx.WorkflowTransitionRecords.Add(new WorkflowTransitionRecord
        {
            Id = guidGenerator.Create(),
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            PreviousState = request.PreviousState,
            NewState = request.NewState,
            TransitionedAt = clock.Now,
            TransitionedBy = request.UserId,
            Comment = request.Comment,
            TenantId = request.TenantId,
        });
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
