namespace Granit.DataExchange.Export;

/// <summary>
/// Default empty filter for export definitions that don't require filtering.
/// Used by <see cref="ExportDefinition{TEntity}"/> (the single-type-parameter overload)
/// and <see cref="IExportDataSource{TEntity}"/>.
/// </summary>
public sealed record EmptyExportFilter
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly EmptyExportFilter Instance = new();
}
