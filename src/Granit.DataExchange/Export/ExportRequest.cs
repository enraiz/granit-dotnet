namespace Granit.DataExchange.Export;

/// <summary>
/// Request to execute a data export.
/// </summary>
/// <param name="DefinitionName">The export definition name (e.g. <c>"Guava.PatientExport"</c>).</param>
/// <param name="Format">Output format (<c>"xlsx"</c> or <c>"csv"</c>).</param>
/// <param name="SelectedFields">
/// Ordered list of field property paths to include.
/// <c>null</c> means all fields from the definition.
/// </param>
/// <param name="IncludeIdForImport">
/// Whether to include the entity ID column for roundtrip import
/// (Odoo <c>"I want to update data"</c> pattern).
/// </param>
/// <param name="Sort">
/// Comma-separated sort specification (e.g. <c>"-createdAt,lastName"</c>).
/// Uses the same syntax as <c>QueryRequest.Sort</c>.
/// <c>null</c> means no sorting (use default order).
/// </param>
/// <param name="Filter">
/// Filter criteria using the <c>filter[field.op]=value</c> syntax.
/// Same format as <c>QueryRequest.Filter</c>.
/// <c>null</c> means no filtering (export all).
/// </param>
/// <param name="Presets">
/// Active preset names, keyed by filter group name.
/// Same format as <c>QueryRequest.Presets</c>.
/// </param>
/// <param name="Search">
/// Free-text search applied to the definition's global search properties.
/// Same as <c>QueryRequest.Search</c>.
/// </param>
public sealed record ExportRequest(
    string DefinitionName,
    string Format,
    IReadOnlyList<string>? SelectedFields,
    bool IncludeIdForImport,
    string? Sort,
    IReadOnlyDictionary<string, string>? Filter,
    IReadOnlyDictionary<string, string>? Presets,
    string? Search);
