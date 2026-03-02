using Granit.DataImport.Mapping;

namespace Granit.DataImport.Endpoints.Dtos;

/// <summary>
/// Request DTO for confirming column mappings on an import job.
/// </summary>
public sealed record ConfirmMappingsRequest(IReadOnlyList<ColumnMapping> Mappings);
