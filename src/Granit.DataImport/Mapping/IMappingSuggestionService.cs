namespace Granit.DataImport.Mapping;

/// <summary>
/// Facade for the 4-tier mapping suggestion pipeline:
/// Saved → Exact → Fuzzy → Semantic (AI).
/// </summary>
/// <remarks>
/// Columns already matched by a higher-confidence tier are excluded from lower tiers.
/// Deduplication keeps the best confidence per source column.
/// </remarks>
public interface IMappingSuggestionService
{
    /// <summary>
    /// Suggests column-to-property mappings for the given file headers.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type (used to resolve the import definition).</typeparam>
    /// <param name="headers">Column headers extracted from the file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of mapping suggestions, one per header that could be matched.
    /// Unmatchable columns are excluded.
    /// </returns>
    Task<IReadOnlyList<ColumnMapping>> SuggestMappingsAsync<TEntity>(
        IReadOnlyList<string> headers,
        CancellationToken ct = default) where TEntity : class;
}
