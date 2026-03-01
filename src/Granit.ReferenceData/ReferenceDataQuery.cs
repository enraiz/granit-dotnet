namespace Granit.ReferenceData;

/// <summary>
/// Query parameters for filtering, sorting, and paginating reference data entries.
/// </summary>
/// <param name="ActiveOnly">When <c>true</c> (default), only active entries are returned.</param>
/// <param name="SearchTerm">Optional text to filter by Code or Label (case-insensitive contains).</param>
/// <param name="SortBy">Property name to sort by (e.g., "Code", "Label", "SortOrder"). Default is "SortOrder".</param>
/// <param name="Descending">When <c>true</c>, sort in descending order. Default is <c>false</c>.</param>
/// <param name="Skip">Number of entries to skip (for pagination).</param>
/// <param name="Take">Maximum number of entries to return (for pagination).</param>
public sealed record ReferenceDataQuery(
    bool ActiveOnly = true,
    string? SearchTerm = null,
    string? SortBy = "SortOrder",
    bool Descending = false,
    int? Skip = null,
    int? Take = null);
