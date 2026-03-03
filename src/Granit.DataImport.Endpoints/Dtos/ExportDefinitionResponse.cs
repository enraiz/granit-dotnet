using Granit.DataImport.Export;

namespace Granit.DataImport.Endpoints.Dtos;

/// <summary>
/// Response DTO for an export definition summary.
/// </summary>
public sealed record ExportDefinitionResponse(
    string Name,
    string EntityType,
    IReadOnlyList<string> SupportedFormats)
{
    /// <summary>
    /// Maps an <see cref="IExportDefinitionDescriptor"/> to a response DTO.
    /// </summary>
    internal static ExportDefinitionResponse FromDescriptor(IExportDefinitionDescriptor descriptor) =>
        new(descriptor.Name, descriptor.EntityType.Name, descriptor.SupportedFormats);
}
