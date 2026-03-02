using Granit.DataImport.Mapping;

namespace Granit.DataImport.Endpoints.Dtos;

/// <summary>
/// Response DTO for the import preview (headers, sample rows, mapping suggestions, and field metadata).
/// </summary>
public sealed record ImportPreviewResponse(
    IReadOnlyList<string> Headers,
    IReadOnlyList<string[]> PreviewRows,
    IReadOnlyList<ColumnMapping> Suggestions,
    IReadOnlyList<FieldMetadata> FieldMetadata);
