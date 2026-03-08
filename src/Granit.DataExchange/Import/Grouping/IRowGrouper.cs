using Granit.DataExchange.Import.Parsing;

namespace Granit.DataExchange.Import.Grouping;

/// <summary>
/// Groups consecutive flat rows sharing the same key into aggregate batches.
/// Sits between <see cref="IFileParser"/> and <c>IDataMapper</c> in the pipeline
/// when declared via <c>ImportDefinitionBuilder.GroupBy()</c>.
/// </summary>
/// <remarks>
/// <para>
/// The file <b>must</b> be sorted by group key. If not, the grouper yields
/// a new group each time the key value changes (it does not sort or buffer the full file).
/// Only the current group is held in memory.
/// </para>
/// </remarks>
public interface IRowGrouper
{
    /// <summary>
    /// Groups consecutive rows by the configured group key.
    /// </summary>
    /// <param name="rows">The raw rows from the file parser.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of grouped rows, one group per aggregate entity.</returns>
    IAsyncEnumerable<GroupedRows> GroupAsync(
        IAsyncEnumerable<RawImportRow> rows,
        CancellationToken cancellationToken = default);
}
