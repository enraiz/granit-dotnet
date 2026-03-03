using Granit.DataExchange.Import.Mapping;

namespace Granit.DataExchange.Endpoints.Dtos.Import;

/// <summary>
/// Request DTO for confirming column mappings on an import job.
/// </summary>
public sealed record ConfirmMappingsRequest(IReadOnlyList<ColumnMapping> Mappings);
