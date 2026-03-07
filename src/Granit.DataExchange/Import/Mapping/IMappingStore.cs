namespace Granit.DataExchange.Import.Mapping;

/// <summary>
/// Persists column mappings for reuse across import jobs.
/// Enables the "Saved" tier of the mapping suggestion pipeline.
/// </summary>
public interface IMappingStore
{
    /// <summary>
    /// Loads previously saved mappings for the given import definition name and tenant.
    /// </summary>
    /// <param name="definitionName">The import definition name (e.g. <c>"Acme.PatientImport"</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Saved column mappings, or an empty list if none exist.</returns>
    Task<IReadOnlyList<ImportColumnMapping>> LoadAsync(
        string definitionName,
        CancellationToken ct = default);

    /// <summary>
    /// Saves the confirmed mappings for future reuse.
    /// </summary>
    /// <param name="definitionName">The import definition name.</param>
    /// <param name="mappings">The confirmed column mappings to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveAsync(
        string definitionName,
        IReadOnlyList<ImportColumnMapping> mappings,
        CancellationToken ct = default);
}
