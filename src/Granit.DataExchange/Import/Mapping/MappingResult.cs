namespace Granit.DataExchange.Import.Mapping;

/// <summary>
/// Result of mapping a raw import row to a typed entity.
/// </summary>
/// <typeparam name="TEntity">The target entity type.</typeparam>
public sealed record MappingResult<TEntity> where TEntity : class
{
    /// <summary>
    /// The mapped entity, or <c>null</c> if conversion failed.
    /// </summary>
    public TEntity? Entity { get; init; }

    /// <summary>
    /// Cell-level conversion errors encountered during mapping.
    /// Empty when <see cref="Succeeded"/> is <c>true</c>.
    /// </summary>
    public IReadOnlyList<CellConversionError> Errors { get; init; } = [];

    /// <summary>
    /// Indicates whether mapping succeeded without errors.
    /// </summary>
    public bool Succeeded => Entity is not null && Errors.Count == 0;
}
