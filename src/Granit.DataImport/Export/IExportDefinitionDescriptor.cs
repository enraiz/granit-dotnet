namespace Granit.DataImport.Export;

/// <summary>
/// Non-generic view of an <see cref="ExportDefinition{TEntity,TFilter}"/> for runtime resolution by name.
/// </summary>
/// <remarks>
/// Registered as a singleton alongside the generic definition by
/// <see cref="ServiceCollectionExtensions.AddExportDefinition{TEntity,TFilter,TDefinition}"/>.
/// Endpoints and other services can enumerate <c>IEnumerable&lt;IExportDefinitionDescriptor&gt;</c>
/// to find a definition by name without compile-time knowledge of the entity type.
/// </remarks>
public interface IExportDefinitionDescriptor
{
    /// <summary>
    /// The definition name (e.g. <c>"Guava.PatientExport"</c>).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The source entity CLR type.
    /// </summary>
    Type EntityType { get; }

    /// <summary>
    /// The filter CLR type (e.g. <c>PatientExportFilter</c>).
    /// </summary>
    Type FilterType { get; }

    /// <summary>
    /// Supported output formats (e.g. <c>["xlsx", "csv"]</c>).
    /// </summary>
    IReadOnlyList<string> SupportedFormats { get; }

    /// <summary>
    /// Gets field descriptors for all declared exportable fields.
    /// </summary>
    IReadOnlyList<ExportFieldDescriptor> GetFields();
}
