using Granit.DataExchange.Import.Parsing;

namespace Granit.DataExchange.Import.Grouping;

/// <summary>
/// A group of consecutive rows sharing the same group key value.
/// Used for parent/child import where a flat file represents aggregate entities.
/// </summary>
/// <param name="GroupKeyValue">Value of the group key (e.g. <c>"CMD-001"</c>).</param>
/// <param name="Rows">All rows belonging to this group, in file order.</param>
public sealed record GroupedRows(
    string GroupKeyValue,
    IReadOnlyList<RawImportRow> Rows);
