namespace Granit.DataExchange.Export;

/// <summary>
/// Provides the base queryable for an export operation.
/// </summary>
/// <typeparam name="TEntity">The source entity type.</typeparam>
/// <remarks>
/// <para>
/// The data source acts as a <b>security guard</b>: it builds the base queryable
/// with tenant isolation, ACL, and any mandatory constraints.
/// Filtering and sorting are delegated to <c>IQueryEngine</c> when the export
/// definition references a <see cref="ExportDefinition{TEntity}.QueryDefinitionName"/>.
/// </para>
/// <para>
/// <b>V1 — Explicit Include():</b> The data source must include all <c>Include()</c>
/// calls for navigation fields declared in the <see cref="ExportDefinition{TEntity}"/>.
/// No auto-include magic. If a navigation field is declared but not included,
/// the exported value will be <c>null</c>.
/// </para>
/// </remarks>
public interface IExportDataSource<out TEntity>
    where TEntity : class
{
    /// <summary>
    /// Returns the base queryable for the export.
    /// Must include <c>Include()</c> calls for navigation properties and apply
    /// security constraints (tenant isolation, ACL).
    /// </summary>
    /// <returns>
    /// A queryable that the export pipeline will filter, sort, and stream.
    /// </returns>
    IQueryable<TEntity> GetQueryable();
}
