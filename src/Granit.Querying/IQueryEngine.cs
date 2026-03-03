using Granit.Querying.Meta;
using Granit.Querying.SavedViews;

namespace Granit.Querying;

/// <summary>
/// Query engine that orchestrates filtering, sorting, pagination, and grouping
/// for a given entity type using a <see cref="QueryDefinition{TEntity}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IQueryEngine<TEntity> where TEntity : class
{
    /// <summary>
    /// Executes a query and returns a paginated result.
    /// </summary>
    /// <param name="source">The base queryable (e.g. from DbContext).</param>
    /// <param name="request">The query parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated result.</returns>
    Task<PagedResult<TEntity>> ExecuteAsync(
        IQueryable<TEntity> source,
        QueryRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a grouped query and returns a grouped result.
    /// </summary>
    /// <param name="source">The base queryable.</param>
    /// <param name="request">The query parameters (must include <see cref="QueryRequest.GroupBy"/>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A grouped result.</returns>
    Task<GroupedResult<TEntity>> ExecuteGroupedAsync(
        IQueryable<TEntity> source,
        QueryRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Generates query metadata from the definition for the <c>GET /meta</c> endpoint.
    /// </summary>
    /// <param name="savedViews">Optional saved views to include in the metadata.</param>
    /// <returns>The query metadata.</returns>
    QueryMetadata GetMetadata(IReadOnlyList<SavedViewSummaryDto>? savedViews = null);
}
