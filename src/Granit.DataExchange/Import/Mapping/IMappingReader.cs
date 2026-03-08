namespace Granit.DataExchange.Import.Mapping;

/// <summary>
/// Reads previously saved column mappings for import definitions.
/// </summary>
public interface IMappingReader
{
    /// <summary>
    /// Loads previously saved mappings for the given import definition name and tenant.
    /// </summary>
    /// <param name="definitionName">The import definition name (e.g. <c>"Acme.PatientImport"</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Saved column mappings, or an empty list if none exist.</returns>
    Task<IReadOnlyList<ImportColumnMapping>> LoadAsync(
        string definitionName,
        CancellationToken cancellationToken = default);
}
