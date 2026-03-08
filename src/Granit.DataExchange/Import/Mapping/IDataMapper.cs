using Granit.DataExchange.Import.Parsing;

namespace Granit.DataExchange.Import.Mapping;

/// <summary>
/// Converts a <see cref="RawImportRow"/> (string values) into a typed entity
/// using the confirmed column mappings.
/// </summary>
/// <typeparam name="TEntity">The target entity type.</typeparam>
public interface IDataMapper<TEntity> where TEntity : class
{
    /// <summary>
    /// Maps a raw row to a typed entity using the provided column mappings.
    /// </summary>
    /// <param name="row">The raw import row with string values.</param>
    /// <param name="mappings">The confirmed column-to-property mappings.</param>
    /// <param name="options">Import options (date format, culture, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A mapping result containing the entity or conversion errors.</returns>
    Task<MappingResult<TEntity>> MapAsync(
        RawImportRow row,
        IReadOnlyList<ImportColumnMapping> mappings,
        ImportOptions options,
        CancellationToken cancellationToken = default);
}
