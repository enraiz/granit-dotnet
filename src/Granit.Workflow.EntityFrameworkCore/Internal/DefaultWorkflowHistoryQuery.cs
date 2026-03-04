using Granit.Workflow.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Granit.Workflow.EntityFrameworkCore.Internal;

/// <summary>
/// Default implementation of <see cref="IWorkflowHistoryQuery"/> using EF Core.
/// Queries <see cref="Domain.WorkflowTransitionRecord"/> entities from the host DbContext
/// that implements <see cref="IWorkflowDbContext"/>.
/// </summary>
internal sealed class DefaultWorkflowHistoryQuery<TDbContext>(TDbContext dbContext)
    : IWorkflowHistoryQuery
    where TDbContext : DbContext, IWorkflowDbContext
{
    private readonly TDbContext _dbContext = dbContext;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TransitionHistoryResponse>> GetHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        List<TransitionHistoryResponse> history = await _dbContext.WorkflowTransitionRecords
            .Where(r => r.EntityType == entityType && r.EntityId == entityId)
            .OrderBy(r => r.TransitionedAt)
            .Select(r => new TransitionHistoryResponse(
                r.PreviousState,
                r.NewState,
                r.TransitionedAt,
                r.TransitionedBy,
                r.Comment))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return history;
    }
}
