using Granit.Querying;
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
    public async Task<PagedResult<TransitionHistoryResponse>> GetHistoryAsync(
        string entityType,
        string entityId,
        int page = 1,
        int pageSize = QueryingDefaults.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        int clampedPage = Math.Max(page, 1);
        int clampedPageSize = Math.Clamp(pageSize, 1, QueryingDefaults.MaxPageSize);

        IQueryable<Domain.WorkflowTransitionRecord> query = _dbContext.WorkflowTransitionRecords
            .Where(r => r.EntityType == entityType && r.EntityId == entityId);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<TransitionHistoryResponse> items = await query
            .OrderBy(r => r.TransitionedAt)
            .Skip((clampedPage - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .Select(r => new TransitionHistoryResponse(
                r.PreviousState,
                r.NewState,
                r.TransitionedAt,
                r.TransitionedBy,
                r.Comment))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PagedResult<TransitionHistoryResponse>(items, totalCount, HasMore: (clampedPage - 1) * clampedPageSize + items.Count < totalCount);
    }
}
