namespace Granit.DataImport.Identity;

/// <summary>
/// Resolves whether an imported entity should be inserted or updated.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// Three concrete strategies are provided in <c>Granit.DataImport.EntityFrameworkCore</c>:
/// <list type="bullet">
///   <item><b>ExternalIdResolver</b>: uses a dedicated external ID mapping table (Odoo <c>__export__</c> pattern).</item>
///   <item><b>BusinessKeyResolver</b>: uses business key properties declared in <c>ImportDefinition&lt;T&gt;</c>.</item>
///   <item><b>CompositeKeyResolver</b>: uses multiple properties combined as a composite key.</item>
/// </list>
/// </remarks>
public interface IRecordIdentityResolver<TEntity> where TEntity : class
{
    /// <summary>
    /// Resolves the identity of a single imported entity.
    /// </summary>
    /// <param name="entity">The mapped entity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The identity resolution result (insert or update with existing entity).</returns>
    Task<RecordIdentity<TEntity>> ResolveAsync(
        TEntity entity,
        CancellationToken ct = default);
}
