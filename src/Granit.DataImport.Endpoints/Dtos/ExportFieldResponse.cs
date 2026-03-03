using Granit.DataImport.Export;

namespace Granit.DataImport.Endpoints.Dtos;

/// <summary>
/// Response DTO for an exportable field descriptor.
/// </summary>
public sealed record ExportFieldResponse(
    string PropertyPath,
    string ClrTypeName,
    string? Header,
    string? Format,
    int Order,
    bool IsNavigation)
{
    /// <summary>
    /// Maps an <see cref="ExportFieldDescriptor"/> to a response DTO.
    /// </summary>
    internal static ExportFieldResponse FromDescriptor(ExportFieldDescriptor field) =>
        new(field.PropertyPath, field.ClrTypeName, field.Header, field.Format, field.Order, field.IsNavigation);
}
