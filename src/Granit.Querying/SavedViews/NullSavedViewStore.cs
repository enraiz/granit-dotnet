namespace Granit.Querying.SavedViews;

/// <summary>
/// Default implementation of <see cref="ISavedViewStore"/>.
/// Throws <see cref="NotImplementedException"/> — requires <c>Granit.Querying.EntityFrameworkCore</c>.
/// </summary>
internal sealed class NullSavedViewStore : ISavedViewStore
{
    private const string Message =
        "Saved view persistence requires Granit.Querying.EntityFrameworkCore. " +
        "Call builder.AddGranitQueryingEntityFrameworkCore() to register a concrete ISavedViewStore.";

    /// <inheritdoc/>
    public Task<IReadOnlyList<SavedView>> GetListAsync(
        string entityType, string userId, Guid? tenantId, CancellationToken ct = default) =>
        throw new NotImplementedException(Message);

    /// <inheritdoc/>
    public Task<SavedView?> GetAsync(Guid id, CancellationToken ct = default) =>
        throw new NotImplementedException(Message);

    /// <inheritdoc/>
    public Task CreateAsync(SavedView view, CancellationToken ct = default) =>
        throw new NotImplementedException(Message);

    /// <inheritdoc/>
    public Task UpdateAsync(SavedView view, CancellationToken ct = default) =>
        throw new NotImplementedException(Message);

    /// <inheritdoc/>
    public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        throw new NotImplementedException(Message);

    /// <inheritdoc/>
    public Task SetDefaultAsync(Guid id, string userId, string entityType, CancellationToken ct = default) =>
        throw new NotImplementedException(Message);
}
