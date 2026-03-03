namespace Granit.DataImport.Export;

/// <summary>
/// Provides the data stream for an export operation,
/// with typed filtering matching the grid's search logic.
/// </summary>
/// <typeparam name="TEntity">The source entity type.</typeparam>
/// <typeparam name="TFilter">
/// The typed filter matching the grid's parameters (search, status, selected IDs, etc.).
/// </typeparam>
/// <remarks>
/// <para>
/// The data source acts as a <b>security guard</b>: it receives a strongly-typed filter
/// object (never raw JSON) and is responsible for applying tenant isolation, ACL,
/// and any mandatory constraints.
/// </para>
/// <para>
/// The data source builds the query (with filters, Includes, AsNoTracking) and returns
/// an <see cref="IAsyncEnumerable{TEntity}"/>. This is the same query as the grid display,
/// minus pagination (<c>Skip</c>/<c>Take</c>).
/// </para>
/// <para>
/// <b>V1 — Explicit Include():</b> The data source must include all <c>Include()</c>
/// calls for navigation fields declared in the <see cref="ExportDefinition{TEntity,TFilter}"/>.
/// No auto-include magic. If a navigation field is declared but not included,
/// the exported value will be <c>null</c>.
/// </para>
/// </remarks>
public interface IExportDataSource<TEntity, in TFilter>
    where TEntity : class
    where TFilter : class
{
    /// <summary>
    /// Streams the filtered entities for export.
    /// </summary>
    /// <param name="filter">The typed filter from the frontend grid.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// An async stream of entities. The export pipeline extracts field values
    /// via reflection based on the <see cref="ExportDefinition{TEntity,TFilter}"/> fields.
    /// </returns>
    IAsyncEnumerable<TEntity> GetDataAsync(TFilter filter, CancellationToken ct = default);
}

/// <summary>
/// Convenience interface for export data sources that don't require filtering.
/// </summary>
/// <typeparam name="TEntity">The source entity type.</typeparam>
public interface IExportDataSource<TEntity> : IExportDataSource<TEntity, EmptyExportFilter>
    where TEntity : class;
