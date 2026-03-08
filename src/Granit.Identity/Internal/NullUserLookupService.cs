using Granit.Identity.Models;
using Granit.Querying;

namespace Granit.Identity.Internal;

/// <summary>
/// Null-object implementation of <see cref="IUserLookupService"/>.
/// Returns <c>null</c> / empty lists / no-ops for all operations.
/// Replaced by <c>CachedUserLookupService</c> when <c>Granit.Identity.EntityFrameworkCore</c> is installed.
/// </summary>
internal sealed class NullUserLookupService : IUserLookupService
{
    /// <inheritdoc/>
    public Task<IdentityUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IdentityUser?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<IdentityUser>> FindByIdsAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IdentityUser>>([]);

    /// <inheritdoc/>
    public Task<PagedResult<IdentityUser>> SearchAsync(
        string searchTerm,
        int page = 1,
        int pageSize = QueryingDefaults.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<IdentityUser>([], 0));

    /// <inheritdoc/>
    public Task<IdentityUser?> RefreshByIdAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IdentityUser?>(null);

    /// <inheritdoc/>
    public Task<int> RefreshAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    /// <inheritdoc/>
    public Task<int> RefreshStaleAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    /// <inheritdoc/>
    public Task DeleteByIdAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task PseudonymizeByIdAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
