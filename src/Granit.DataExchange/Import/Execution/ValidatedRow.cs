using Granit.DataExchange.Import.Identity;

namespace Granit.DataExchange.Import.Execution;

/// <summary>
/// A row that has passed mapping and validation, ready for persistence.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="RowNumber">The source row number.</param>
/// <param name="Entity">The validated entity instance.</param>
/// <param name="Identity">The identity resolution result, or <c>null</c> for insert-only imports.</param>
public sealed record ValidatedRow<TEntity>(
    int RowNumber,
    TEntity Entity,
    RecordIdentity<TEntity>? Identity = null) where TEntity : class;
