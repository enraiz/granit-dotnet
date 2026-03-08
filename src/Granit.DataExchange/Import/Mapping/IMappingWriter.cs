namespace Granit.DataExchange.Import.Mapping;

/// <summary>
/// Persists confirmed column mappings for reuse across import jobs.
/// </summary>
public interface IMappingWriter
{
    /// <summary>
    /// Saves the confirmed mappings for future reuse.
    /// </summary>
    /// <param name="definitionName">The import definition name.</param>
    /// <param name="mappings">The confirmed column mappings to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(
        string definitionName,
        IReadOnlyList<ImportColumnMapping> mappings,
        CancellationToken cancellationToken = default);
}
