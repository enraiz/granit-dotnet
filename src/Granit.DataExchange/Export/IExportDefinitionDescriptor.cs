namespace Granit.DataExchange.Export;

/// <summary>
/// Non-generic view of an <see cref="ExportDefinition{TEntity}"/> for runtime resolution by name.
/// </summary>
/// <remarks>
/// Registered as a singleton alongside the generic definition by
/// <see cref="ServiceCollectionExtensions.AddExportDefinition{TEntity,TDefinition}"/>.
/// Endpoints and other services can enumerate <c>IEnumerable&lt;IExportDefinitionDescriptor&gt;</c>
/// to find a definition by name without compile-time knowledge of the entity type.
/// </remarks>
public interface IExportDefinitionDescriptor
{
    /// <summary>
    /// The definition name (e.g. <c>"Acme.PatientExport"</c>).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The source entity CLR type.
    /// </summary>
    Type EntityType { get; }

    /// <summary>
    /// Name of the associated <c>QueryDefinition</c> for filtering and sorting.
    /// <c>null</c> when no query-based filtering is needed.
    /// </summary>
    string? QueryDefinitionName { get; }

    /// <summary>
    /// Supported output formats (e.g. <c>["xlsx", "csv"]</c>).
    /// </summary>
    IReadOnlyList<string> SupportedFormats { get; }

    /// <summary>
    /// Gets field descriptors for all declared exportable fields.
    /// </summary>
    IReadOnlyList<ExportFieldDescriptor> GetFields();
}
