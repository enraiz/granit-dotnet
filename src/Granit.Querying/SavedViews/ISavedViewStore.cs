namespace Granit.Querying.SavedViews;

/// <summary>
/// Persistence abstraction for saved views.
/// Default implementation is a null-object that throws; use
/// <c>Granit.Querying.EntityFrameworkCore</c> for a concrete store.
/// </summary>
public interface ISavedViewStore
{
    /// <summary>
    /// Gets all saved views for an entity type visible to the user.
    /// Includes personal views and shared views.
    /// </summary>
    /// <param name="entityType">The query definition name.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<SavedView>> GetListAsync(
        string entityType, string userId, Guid? tenantId, CancellationToken ct = default);

    /// <summary>
    /// Gets a single saved view by identifier.
    /// </summary>
    /// <param name="id">The saved view identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<SavedView?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new saved view.
    /// </summary>
    /// <param name="view">The saved view to create.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateAsync(SavedView view, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing saved view.
    /// </summary>
    /// <param name="view">The saved view to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(SavedView view, CancellationToken ct = default);

    /// <summary>
    /// Deletes a saved view by identifier.
    /// </summary>
    /// <param name="id">The saved view identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Sets a saved view as the default for a user and entity type.
    /// Unsets any previous default for the same user and entity type.
    /// </summary>
    /// <param name="id">The saved view identifier to set as default.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="entityType">The query definition name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetDefaultAsync(Guid id, string userId, string entityType, CancellationToken ct = default);
}
