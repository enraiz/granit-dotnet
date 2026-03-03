using System.Text.Json;

namespace Granit.DataExchange.Endpoints.Dtos.Export;

/// <summary>
/// Request DTO for creating an export job.
/// </summary>
/// <param name="DefinitionName">The export definition name (e.g. <c>"Guava.PatientExport"</c>).</param>
/// <param name="Format">Output format (<c>"xlsx"</c> or <c>"csv"</c>).</param>
/// <param name="SelectedFields">
/// Ordered list of field property paths to include. <c>null</c> means all fields.
/// </param>
/// <param name="IncludeIdForImport">Whether to include the entity ID for roundtrip import.</param>
/// <param name="Filter">
/// Typed filter matching the definition's <c>TFilter</c> type (JSON, deserialized server-side).
/// </param>
public sealed record CreateExportJobRequest(
    string DefinitionName,
    string Format,
    IReadOnlyList<string>? SelectedFields,
    bool IncludeIdForImport,
    JsonElement? Filter);
