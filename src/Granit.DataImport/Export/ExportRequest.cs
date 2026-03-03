using System.Text.Json;

namespace Granit.DataImport.Export;

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
/// <param name="FilterJson">
/// Serialized filter matching the definition's <c>TFilter</c> type.
/// Deserialized by the orchestrator into the strongly-typed filter object.
/// <c>null</c> means no filtering (export all).
/// </param>
public sealed record ExportRequest(
    string DefinitionName,
    string Format,
    IReadOnlyList<string>? SelectedFields,
    bool IncludeIdForImport,
    JsonElement? FilterJson);
