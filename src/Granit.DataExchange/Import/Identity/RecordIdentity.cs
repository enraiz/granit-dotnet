namespace Granit.DataExchange.Import.Identity;

/// <summary>
/// Result of resolving the identity of an imported record.
/// Determines whether the record should be inserted or updated.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed record RecordIdentity<TEntity> where TEntity : class
{
    /// <summary>
    /// The persistence operation to perform.
    /// </summary>
    public required RecordOperation Operation { get; init; }

    /// <summary>
    /// The existing entity from the database, or <c>null</c> for <see cref="RecordOperation.Insert"/>.
    /// When non-null (<see cref="RecordOperation.Update"/>), the imported values
    /// should be applied to this instance.
    /// </summary>
    public TEntity? ExistingEntity { get; init; }
}
